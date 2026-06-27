using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlackPDF.Core;
using SlackPDF.Core.Engines;
using SlackPDF.Services;
using System.ComponentModel;

namespace SlackPDF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _activeEngineName = "PDFsharp";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private bool _currentViewIsBusy;
    [ObservableProperty] private string _selectedNavItem = "Merge";

    private ObservableObject? _previousView;

    public MergeViewModel    MergeVm    { get; }
    public SplitViewModel    SplitVm    { get; }
    public MixViewModel      MixVm      { get; }
    public RotateViewModel   RotateVm   { get; }
    public ExtractViewModel  ExtractVm  { get; }
    public InsertViewModel   InsertVm   { get; }
    public ComposerViewModel ComposerVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public MainViewModel()
    {
        var settings = SettingsService.Load();
        IPdfEngine engine = new PdfSharpEngine();
        var ops   = new PdfOperations(engine);
        var thumbs = new ThumbnailService(settings.ThumbnailCache);
        ActiveEngineName = engine.Name;

        MergeVm    = new MergeViewModel(ops);
        SplitVm    = new SplitViewModel(ops);
        MixVm      = new MixViewModel(ops);
        RotateVm   = new RotateViewModel(ops, thumbs);
        ExtractVm  = new ExtractViewModel(ops, thumbs);
        InsertVm   = new InsertViewModel(ops);
        ComposerVm = new ComposerViewModel(ops, thumbs);
        SettingsVm = new SettingsViewModel(ops, thumbs);

        CurrentView = MergeVm;
    }

    partial void OnCurrentViewChanged(ObservableObject? value)
    {
        if (_previousView is BaseOperationViewModel oldVm)
            oldVm.PropertyChanged -= OnCurrentVmPropertyChanged;

        _previousView = value;

        if (value is BaseOperationViewModel newVm)
        {
            newVm.PropertyChanged += OnCurrentVmPropertyChanged;
            CurrentViewIsBusy = newVm.IsBusy;
        }
        else
        {
            CurrentViewIsBusy = false;
        }
    }

    private void OnCurrentVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsBusy" && sender is BaseOperationViewModel vm)
            CurrentViewIsBusy = vm.IsBusy;
    }

    [RelayCommand]
    private void Navigate(string target)
    {
        SelectedNavItem = target;
        CurrentView = target switch
        {
            "Merge"    => MergeVm,
            "Split"    => SplitVm,
            "Mix"      => MixVm,
            "Rotate"   => RotateVm,
            "Extract"  => ExtractVm,
            "Insert"   => InsertVm,
            "Composer" => ComposerVm,
            "Settings" => SettingsVm,
            _ => MergeVm
        };
    }
}
