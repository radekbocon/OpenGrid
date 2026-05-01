using SimLab.Models;

namespace SimLab.ViewModels;

public class SessionDetailsViewModel : ViewModelBase
{
    public Session? Session { get; set; }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Session session)
        {
            Session = session;
        }
    }
}