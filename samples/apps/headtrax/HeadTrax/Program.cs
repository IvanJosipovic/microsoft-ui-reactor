using Duct;
using Duct.Core;
using HeadTrax;
using HeadTrax.Components;

// Parse --db and --graphql-url from command line
var cliArgs = Environment.GetCommandLineArgs();
var dbIdx = Array.IndexOf(cliArgs, "--db");
if (dbIdx >= 0 && dbIdx + 1 < cliArgs.Length)
    AppConfig.SqliteDbPath = cliArgs[dbIdx + 1];

var urlIdx = Array.IndexOf(cliArgs, "--graphql-url");
if (urlIdx >= 0 && urlIdx + 1 < cliArgs.Length)
    AppConfig.GraphQLUrl = cliArgs[urlIdx + 1];

DuctApp.Run<App>("HeadTrax – Employee Database", width: 1400, height: 900, preview: true);
