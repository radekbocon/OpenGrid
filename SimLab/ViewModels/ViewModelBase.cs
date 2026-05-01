using CommunityToolkit.Mvvm.ComponentModel;

namespace SimLab.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual void SetParameters(params object[] parameters)
    {
    }
}