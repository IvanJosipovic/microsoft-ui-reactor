using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.UI.Reactor.Analyzers;

/// <summary>
/// <c>REACTOR_DOCK_001</c> — flags the <c>DockManager</c> round-trip footgun:
/// assigning <c>OnLiveLayoutChanged</c> a lambda that forwards its parameter
/// straight back into a state setter.
///
/// <para>
/// The host owns the user's drag-modified <em>shape</em> internally (spec 045
/// §2.30) and resolves content by <c>Key</c> from <c>manager.Layout</c> each
/// render. Feeding the live layout back into app state double-owns the shape:
/// drag-back-to-redock breaks, tab selection resets, splitter drags snap back.
/// <c>OnLiveLayoutChanged</c> is for <strong>observation only</strong>.
/// </para>
///
/// <para>
/// Heuristic (mirrors <see cref="MissingWithKeyAnalyzer"/>'s conservative,
/// syntax-only style): an assignment whose left side names
/// <c>OnLiveLayoutChanged</c> and whose right side is a single-parameter lambda
/// whose body <em>forwards the parameter straight into</em>:
/// </para>
/// <list type="bullet">
///   <item>a bare-identifier invocation — <c>next =&gt; setLayout(next)</c>
///   (the UseState setter shape); member-access calls such as
///   <c>Console.WriteLine(next)</c> or <c>inspector.Update(next)</c> are
///   treated as observation/logging and ignored, or</item>
///   <item>an assignment to a <c>Layout</c> target —
///   <c>next =&gt; manager.Layout = next</c>.</item>
/// </list>
/// Empty bodies, and bodies that don't forward the parameter, never fire.
///
/// <para>
/// The same heuristic also fires on the idiomatic fluent modifier —
/// <c>manager.LiveLayoutChanged(next =&gt; setLayout(next))</c> — which wires the
/// handler through an invocation rather than a raw assignment.
/// </para>
///
/// <para>
/// Note: observation routed through a <em>local delegate or method</em> —
/// <c>next =&gt; observe(next)</c> — is intentionally NOT exempt and will warn,
/// because syntactically it is indistinguishable from a state setter. Only
/// member-access calls (<c>inspector.Update(next)</c>) are treated as
/// observation; route observation through a named object to avoid the warning.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OnLiveLayoutRoundTripAnalyzer : DiagnosticAnalyzer
{
    public const string Id = "REACTOR_DOCK_001";

    private const string HandlerName = "OnLiveLayoutChanged";

    private const string ModifierName = "LiveLayoutChanged";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        "OnLiveLayoutChanged feeds the live layout back into state",
        "OnLiveLayoutChanged forwards the host's live layout straight into a setter. The host already owns the drag-modified shape — round-tripping it double-owns the layout and breaks re-docking, tab selection, and splitter drags. OnLiveLayoutChanged is for observation only.",
        "Reactor.Docking",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Per the reactor-docking skill — the app owns content (which panes exist, keyed via manager.Layout) and the host owns shape (drag arrangement). Never store the host's live layout in app state. Reset is a .WithKey($\"dock-{epoch}\") remount; persistence is PersistenceId. OnLiveLayoutChanged should only observe (e.g. a layout inspector).");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeAssignment(SyntaxNodeAnalysisContext ctx)
    {
        var assignment = (AssignmentExpressionSyntax)ctx.Node;

        // Left side must name the handler — either `OnLiveLayoutChanged = ...`
        // (object initializer) or `manager.OnLiveLayoutChanged = ...`.
        var leftName = assignment.Left switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
            _ => null,
        };
        if (leftName != HandlerName) return;

        // Right side must be a forwarding single-parameter lambda.
        if (IsForwardingLambda(assignment.Right))
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation()));
    }

    // The idiomatic fluent modifier `manager.LiveLayoutChanged(next => setLayout(next))`
    // wires the handler through an invocation rather than a raw assignment.
    static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        // Target must be a member access named `LiveLayoutChanged`.
        if (invocation.Expression is not MemberAccessExpressionSyntax member) return;
        if (member.Name.Identifier.ValueText != ModifierName) return;

        // Exactly one argument, a forwarding single-parameter lambda.
        if (invocation.ArgumentList.Arguments.Count != 1) return;
        if (IsForwardingLambda(invocation.ArgumentList.Arguments[0].Expression))
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
    }

    // True when `expr` is a single-parameter lambda (simple or parenthesized)
    // whose body forwards the parameter straight into a state setter. A bare
    // `handler` identifier (the modifier definition `el with { ... = handler }`)
    // is not a lambda and never fires.
    static bool IsForwardingLambda(ExpressionSyntax expr)
    {
        var (parameter, body) = expr switch
        {
            SimpleLambdaExpressionSyntax simple => (simple.Parameter.Identifier.ValueText, simple.Body),
            ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
                => (paren.ParameterList.Parameters[0].Identifier.ValueText, paren.Body),
            _ => (null, (CSharpSyntaxNode?)null),
        };
        if (parameter is null || body is null) return false;

        // Unwrap a single-statement block: `next => { setLayout(next); }`.
        if (body is BlockSyntax block)
        {
            if (block.Statements.Count != 1) return false;
            // An Action lambda can only forward via an expression statement;
            // a value-returning `return` arm is unreachable for a void delegate.
            if (block.Statements[0] is not ExpressionStatementSyntax stmt) return false;
            body = stmt.Expression;
        }

        return ForwardsParameter(body as ExpressionSyntax, parameter);
    }

    // True when `expr` hands `parameter` straight into a state setter — either a
    // bare-identifier invocation (`setLayout(p)`) or an assignment to a `Layout`
    // target (`x.Layout = p`). Member-access invocations (Console.WriteLine(p),
    // inspector.Update(p)) are observation/logging and deliberately excluded.
    static bool ForwardsParameter(ExpressionSyntax? expr, string parameter)
    {
        switch (expr)
        {
            case InvocationExpressionSyntax inv:
                if (inv.Expression is not IdentifierNameSyntax) return false;
                return IsSingleArgEqualToParameter(inv, parameter);

            case AssignmentExpressionSyntax inner
                when inner.IsKind(SyntaxKind.SimpleAssignmentExpression):
                var targetName = inner.Left switch
                {
                    IdentifierNameSyntax id => id.Identifier.ValueText,
                    MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                    _ => null,
                };
                return targetName == "Layout" && IsIdentifier(inner.Right, parameter);

            default:
                return false;
        }
    }

    static bool IsSingleArgEqualToParameter(InvocationExpressionSyntax inv, string parameter)
    {
        if (inv.ArgumentList.Arguments.Count != 1) return false;
        return IsIdentifier(inv.ArgumentList.Arguments[0].Expression, parameter);
    }

    static bool IsIdentifier(ExpressionSyntax expr, string name)
        => expr is IdentifierNameSyntax id && id.Identifier.ValueText == name;
}
