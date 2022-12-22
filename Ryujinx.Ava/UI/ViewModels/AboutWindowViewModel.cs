namespace Ryujinx.Ava.UI.ViewModels;

public class AboutWindowViewModel : BaseModel
{
    private string _supporters;
    private string _version;
    private string _developers;

    public string Supporters
    {
        get => _supporters;
        set
        {
            _supporters = value;
            OnPropertyChanged();
        }
    }

    public string Version
    {
        get => _version;
        set
        {
            _version = value;
            OnPropertyChanged();
        }
    }

    public string Developers
    {
        get => _developers;
        set
        {
            _developers = value;
            OnPropertyChanged();
        }
    }
}