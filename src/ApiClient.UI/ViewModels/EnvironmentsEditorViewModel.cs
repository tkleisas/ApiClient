using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ApiClient.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiClient.UI.ViewModels;

/// <summary>One editable variable row (toggle, name, value).</summary>
public partial class KeyValueRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>An editable environment: a name and a list of variable rows.</summary>
public partial class EnvironmentEditViewModel : ObservableObject
{
    /// <summary>Creates an empty, unnamed environment.</summary>
    public EnvironmentEditViewModel()
        : this(new ApiEnvironment { Name = "New environment" })
    {
    }

    /// <summary>Creates an editable copy of <paramref name="environment"/>.</summary>
    public EnvironmentEditViewModel(ApiEnvironment environment)
    {
        Name = environment.Name;
        foreach (var variable in environment.Variables)
            Variables.Add(new KeyValueRowViewModel { Enabled = variable.Enabled, Name = variable.Name, Value = variable.Value });
    }

    [ObservableProperty]
    private string _name;

    /// <summary>The variable rows.</summary>
    public ObservableCollection<KeyValueRowViewModel> Variables { get; } = [];

    [RelayCommand]
    private void AddVariable() => Variables.Add(new KeyValueRowViewModel());

    [RelayCommand]
    private void RemoveVariable(KeyValueRowViewModel row) => Variables.Remove(row);

    /// <summary>Produces an <see cref="ApiEnvironment"/> from the current edits (rows with a non-empty name).</summary>
    public ApiEnvironment ToEnvironment() => new ApiEnvironment
    {
        Name = Name,
        Variables = Variables
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .Select(v => new KeyValueItem(v.Name, v.Value, v.Enabled))
            .ToList(),
    };
}

/// <summary>The environments dialog: a list of environments, each with editable variables.</summary>
public partial class EnvironmentsEditorViewModel : ViewModelBase
{
    /// <summary>Design-time constructor.</summary>
    public EnvironmentsEditorViewModel()
        : this([])
    {
    }

    /// <summary>Creates the editor seeded from existing environments.</summary>
    public EnvironmentsEditorViewModel(IEnumerable<ApiEnvironment> environments)
    {
        foreach (var environment in environments)
            Environments.Add(new EnvironmentEditViewModel(environment));
        SelectedEnvironment = Environments.FirstOrDefault();
    }

    /// <summary>The environments being edited.</summary>
    public ObservableCollection<EnvironmentEditViewModel> Environments { get; } = [];

    [ObservableProperty]
    private EnvironmentEditViewModel? _selectedEnvironment;

    [RelayCommand]
    private void AddEnvironment()
    {
        var environment = new EnvironmentEditViewModel();
        Environments.Add(environment);
        SelectedEnvironment = environment;
    }

    [RelayCommand]
    private void RemoveEnvironment()
    {
        if (SelectedEnvironment is not null)
            Environments.Remove(SelectedEnvironment);
        SelectedEnvironment = Environments.FirstOrDefault();
    }

    /// <summary>Produces the edited environments (those with a non-empty name).</summary>
    public IReadOnlyList<ApiEnvironment> ToEnvironments() => Environments
        .Where(e => !string.IsNullOrWhiteSpace(e.Name))
        .Select(e => e.ToEnvironment())
        .ToList();
}
