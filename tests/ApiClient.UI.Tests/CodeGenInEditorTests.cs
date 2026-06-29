using System.Linq;
using ApiClient.Core.CodeGen;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class CodeGenInEditorTests
{
    [Fact]
    public void Offers_both_client_and_server_generators()
    {
        var vm = new RequestEditorViewModel();

        Assert.Contains(vm.Generators, g => g.Scenario == CodeGenScenario.Client);
        Assert.Contains(vm.Generators, g => g.Scenario == CodeGenScenario.Server);
    }

    [Fact]
    public void Generates_client_code_for_the_current_request()
    {
        var vm = new RequestEditorViewModel { Url = "https://h/users" };
        vm.SelectedGenerator = vm.Generators.First(g => g.Scenario == CodeGenScenario.Client);

        vm.GenerateCodeCommand.Execute(null);

        Assert.Contains("HttpClient", vm.GeneratedCode);
    }

    [Fact]
    public void Generates_server_code_when_the_server_generator_is_selected()
    {
        var vm = new RequestEditorViewModel { Url = "https://h/users", Method = "GET" };
        vm.SelectedGenerator = vm.Generators.First(g => g.Scenario == CodeGenScenario.Server);

        vm.GenerateCodeCommand.Execute(null);

        Assert.Contains("app.MapGet(\"/users\"", vm.GeneratedCode);
    }
}
