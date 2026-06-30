using System.Linq;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class EnvironmentsEditorTests
{
    [Fact]
    public void Round_trips_environments_and_variables()
    {
        var editor = new EnvironmentsEditorViewModel(
        [
            new ApiEnvironment { Name = "Local", Variables = [new KeyValueItem("baseUrl", "https://localhost")] },
        ]);

        var result = editor.ToEnvironments();

        var local = Assert.Single(result);
        Assert.Equal("Local", local.Name);
        Assert.Equal("https://localhost", local.ToVariableMap()["baseUrl"]);
    }

    [Fact]
    public void Add_environment_then_edit_variables_is_captured()
    {
        var editor = new EnvironmentsEditorViewModel([]);
        editor.AddEnvironmentCommand.Execute(null);
        editor.SelectedEnvironment!.Name = "UAT";
        editor.SelectedEnvironment.AddVariableCommand.Execute(null);
        var row = editor.SelectedEnvironment.Variables.Last();
        row.Name = "baseUrl";
        row.Value = "https://uat";

        var result = editor.ToEnvironments();

        var uat = Assert.Single(result);
        Assert.Equal("UAT", uat.Name);
        Assert.Equal("https://uat", uat.ToVariableMap()["baseUrl"]);
    }

    [Fact]
    public void Drops_variable_rows_without_a_name()
    {
        var editor = new EnvironmentsEditorViewModel(
        [
            new ApiEnvironment { Name = "Local", Variables = [new KeyValueItem("", "orphan")] },
        ]);

        Assert.Empty(editor.ToEnvironments().Single().Variables);
    }
}
