using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<FormsApp>("Forms", width: 600, height: 700
#if DEBUG
    , preview: true
#endif
);

// <snippet:controlled-input>
class ControlledInputDemo : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");

        return VStack(12,
            SubHeading("Controlled Input"),
            TextField(name, setName, placeholder: "Type your name"),
            Text($"You typed: {name}").Opacity(0.6)
        ).Padding(24);
    }
}
// </snippet:controlled-input>

// <snippet:input-types>
class InputTypesDemo : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("");
        var (password, setPassword) = UseState("");
        var (volume, setVolume) = UseState(50.0);
        var (count, setCount) = UseState(1.0);
        var (agree, setAgree) = UseState(false);
        var (notify, setNotify) = UseState(true);
        var (role, setRole) = UseState(0);
        var (priority, setPriority) = UseState(0);

        return VStack(12,
            TextField(text, setText, placeholder: "Email",
                header: "Email"),
            PasswordBox(password, setPassword,
                placeholderText: "Enter password"),
            Slider(volume, 0, 100, setVolume),
            NumberBox(count, setCount, header: "Quantity"),
            CheckBox(agree, setAgree, label: "I agree to the terms"),
            ToggleSwitch(notify, setNotify,
                header: "Notifications"),
            ComboBox(["Admin", "Editor", "Viewer"],
                role, setRole),
            RadioButtons(["Low", "Medium", "High"],
                priority, setPriority)
        ).Padding(24);
    }
}
// </snippet:input-types>

// <snippet:validation>
class ValidationDemo : Component
{
    public override Element Render()
    {
        var (email, setEmail) = UseState("");
        var (age, setAge) = UseState(0.0);

        var emailValid = email.Contains('@') && email.Contains('.');
        var ageValid = age >= 18 && age <= 120;
        var formValid = emailValid && ageValid
            && !string.IsNullOrWhiteSpace(email);

        return VStack(12,
            SubHeading("Validation"),
            TextField(email, setEmail, placeholder: "user@example.com",
                header: "Email"),
            When(!string.IsNullOrEmpty(email) && !emailValid, () =>
                Text("Enter a valid email address")
                    .Foreground("#d13438").FontSize(12)),
            NumberBox(age, setAge, header: "Age"),
            When(age > 0 && !ageValid, () =>
                Text("Age must be between 18 and 120")
                    .Foreground("#d13438").FontSize(12)),
            Button("Submit", () => { })
                .Disabled(!formValid)
                .Margin(0, 8, 0, 0)
        ).Padding(24);
    }
}
// </snippet:validation>

// <snippet:registration-form>
class RegistrationForm : Component
{
    public override Element Render()
    {
        var (username, setUsername) = UseState("");
        var (email, setEmail) = UseState("");
        var (password, setPassword) = UseState("");
        var (role, setRole) = UseState(0);
        var (acceptTerms, setAcceptTerms) = UseState(false);
        var isValid = !string.IsNullOrWhiteSpace(username)
            && email.Contains('@') && password.Length >= 8 && acceptTerms;

        return VStack(12,
            Heading("Create Account"),
            TextField(username, setUsername, placeholder: "Choose a username",
                header: "Username"),
            TextField(email, setEmail, placeholder: "you@example.com",
                header: "Email"),
            PasswordBox(password, setPassword, placeholderText: "Min 8 chars"),
            When(password.Length > 0 && password.Length < 8, () =>
                Text("Password too short").Foreground("#d13438").FontSize(12)),
            ComboBox(["Developer", "Designer", "Manager"], role, setRole),
            CheckBox(acceptTerms, setAcceptTerms,
                label: "I accept the terms of service"),
            Button("Register", () => { }).Disabled(!isValid)
        ).Padding(24);
    }
}
// </snippet:registration-form>

class FormsApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Forms and Input"),
                Component<ControlledInputDemo>(),
                Component<InputTypesDemo>(),
                Component<ValidationDemo>(),
                Component<RegistrationForm>()
            ).Padding(24)
        );
    }
}
