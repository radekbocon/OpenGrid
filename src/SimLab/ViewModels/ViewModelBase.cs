using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace SimLab.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public bool IsMenuItem { get; protected init; }
    public ICommand LoadedCommand { get; }
    public ICommand UnloadedCommand { get; }
    
    public ViewModelBase()
    {
        LoadedCommand = new AsyncRelayCommand(OnLoadedAsync);
        UnloadedCommand = new AsyncRelayCommand(OnUnloadedAsync);
    }


    protected virtual Task OnLoadedAsync()
    {
        Log.Information("Loaded {0}", GetType().Name);
        
        return Task.CompletedTask;
    }
    
    protected virtual Task OnUnloadedAsync()
    {
        Log.Information("Unloaded {0}", GetType().Name);
        
        return Task.CompletedTask;
    }

    public virtual void SetParameters(params object[] parameters)
    {
    }
}