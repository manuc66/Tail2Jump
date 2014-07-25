using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Be.Manuc66.Library.General;
using Be.Manuc66.Library.WPF;
using Tail2Jump.Config.Objects;
using Tail2Jump.Enums;
using Tail2Jump.Objects;

namespace Tail2Jump.GUI
{
  /// <summary>
  /// Logique d'interaction pour CreateEditWatcher.xaml
  /// </summary>
  public partial class CreateEditWatcher : Window, INotifyPropertyChanged
  {
    #region Members
    private ThreadSafeObservableCollection<string> m_FilterList;
    private ThreadSafeObservableCollection<string> m_LocationList;
    private MonitoringInstance m_Monitoring;
    #endregion // Members

    #region Properties

    public event PropertyChangedEventHandler PropertyChanged;
    public ThreadSafeObservableCollection<string> FilterList
    {
      get { return m_FilterList; }
      set
      {
        if (m_FilterList != value)
        {
          m_FilterList = value;
          OnPropertyChanged("FilterList");
        }
      }
    }
    public ThreadSafeObservableCollection<string> LocationList
    {
      get { return m_LocationList; }
      set
      {
        if (m_LocationList != value)
        {
          m_LocationList = value;
          OnPropertyChanged("LocationList");
        }
      }
    }
    public MonitoringInstance Monitoring
    {
      get { return m_Monitoring; }
      set
      {
        if (m_Monitoring != value)
        {
          m_Monitoring = value;
          OnPropertyChanged("Monitoring");
        }
      }
    }
    public FormReturnCode ReturnCode { get; set; }

    #region Commands

    public ICommand<IEmpty> SelectDirectoryCommand { get; private set; }
    public bool CanBrowse
    {
      get
      {
        return m_CanBrowse;
      }
      set
      {
        if (m_CanBrowse != value)
        {
          m_CanBrowse = value;
          OnPropertyChanged("CanBrowse");
          SelectDirectoryCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool m_CanBrowse = true;
    public ICommand<IEmpty> StartCommand { get; private set; }
    public bool CanStart
    {
      get
      {
        return m_CanStart;
      }
      set
      {
        if (m_CanStart != value)
        {
          m_CanStart = value;
          OnPropertyChanged("CanStart");
          StartCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool m_CanStart = true;
    public ICommand<IEmpty> CancelCommand { get; private set; }
    public bool CanCancel
    {
      get
      {
        return m_CanCancel;
      }
      set
      {
        if (m_CanCancel != value)
        {
          m_CanCancel = value;
          OnPropertyChanged("CanCancel");
          CancelCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool m_CanCancel = true;

    #endregion // Commands
    #endregion // Properties

    #region Dependency Properties

    #endregion // Dependency Properties

    #region Constructor
    public CreateEditWatcher()
    {
      InitializeComponent();

      DataContext = this;

      InitCreateEditWatcher();


    }
    #endregion // Constructor

    #region Methods
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }

    private bool InitCreateEditWatcher()
    {
      bool result = false;
      ReturnCode = FormReturnCode.None;
      Monitoring = new MonitoringInstance();
      SelectDirectoryCommand = new SimpleCommand<IEmpty>(e => CanBrowse, e => SelectDirectory());

      StartCommand = new SimpleCommand<IEmpty>(e => CanStart, e => Start());

      CancelCommand = new SimpleCommand<IEmpty>(e => CanCancel, e => Cancel());

      FilterList = new ThreadSafeObservableCollection<string>();
      LocationList = new ThreadSafeObservableCollection<string>();

      CFG.Load();
      if (CFG.Conf != null)
      {
        if (CFG.Conf.FilterHistory != null && CFG.Conf.FilterHistory.Count > 0)
        {
          foreach (var search in CFG.Conf.FilterHistory)
          {
            FilterList.Add(search);
          }
        }

        if (CFG.Conf.DirectoryHistory != null && CFG.Conf.DirectoryHistory.Count > 0)
        {
          foreach (var dir in CFG.Conf.DirectoryHistory)
          {
            LocationList.Add(dir);
          }
        }

        Monitoring.Filter = CFG.Conf.FilterHistory.FirstOrDefault();
        Monitoring.FullDirectoryPath = CFG.Conf.DirectoryHistory.FirstOrDefault();
      }
      if (string.IsNullOrEmpty(Monitoring.Filter))
      {
        Monitoring.Filter = "*.log";
      }
      if (string.IsNullOrEmpty(Monitoring.FullDirectoryPath))
      {
        Monitoring.FullDirectoryPath = @"C:\Logs\Demo";
      }

      result = true;
      return true;
    }

    private void Start()
    {
      if (string.IsNullOrWhiteSpace(Monitoring.FullDirectoryPath) && !Directory.Exists(Monitoring.FullDirectoryPath))
      {
        return;
      }
      ReturnCode = FormReturnCode.Start;
      // Sauvegarde de tout ce qui se trouve dans CFG.
      CFG.Load();
      Config.Objects.Config oldConf = CFG.Conf;

      CFG.Conf = new Config.Objects.Config();

      string tempFullDirectoryPath = Monitoring.FullDirectoryPath;
      LocationList.Remove(Monitoring.FullDirectoryPath);
      LocationList.Insert(0, Monitoring.FullDirectoryPath);
      Monitoring.FullDirectoryPath = tempFullDirectoryPath;

      string tmpFilter = Monitoring.Filter;
      FilterList.Remove(Monitoring.Filter);
      FilterList.Insert(0, Monitoring.Filter);
      Monitoring.Filter = tmpFilter;

      if (oldConf != null)
      {
        MergeList(oldConf.DirectoryHistory, LocationList, CFG.Conf.DirectoryHistory);
        MergeList(oldConf.FilterHistory, FilterList, CFG.Conf.FilterHistory);
      }
      
      var lastSearch = new CriterianSearch
        {
          FileExtension = Monitoring.Filter,
          PathDirectory = Monitoring.FullDirectoryPath,
          Recursive = Monitoring.IncludeSubdirectories
        };
      CFG.Conf.Search.Add(lastSearch);
      CFG.Save();

      CloseForm();
    }
    private static void AddUnique<T>(IEnumerable<T> from, ICollection<T> to)
    {
      foreach (var item in from)
      {
        if (!to.Contains(item))
        {
          to.Add(item);
        }
      }
    }
    private static void MergeList(IEnumerable<string> oldConf, IEnumerable<string> current, ICollection<string> newConf)
    {
      AddUnique(current, newConf);
      AddUnique(oldConf, newConf);
    }


    private void Cancel()
    {
      ReturnCode = FormReturnCode.Cancel;
      CloseForm();
    }

    private void CloseForm()
    {
      Close();
    }

    private bool SelectDirectory()
    {
      bool result = false;
      string tempPath = string.Empty;

      using (var dialog = new FolderBrowserDialog())
      {
        if (Monitoring != null && Monitoring.FullDirectoryPath != string.Empty)
        {
          tempPath = Monitoring.FullDirectoryPath;
        }
        else
        {
          if (LocationList != null && LocationList.Count > 0)
          {
            tempPath = LocationList[0];
          }
        }

        dialog.SelectedPath = tempPath;
        DialogResult dialogResult = dialog.ShowDialog();

        if (dialogResult == System.Windows.Forms.DialogResult.OK)
        {
          Monitoring.FullDirectoryPath = dialog.SelectedPath;
        }
      }

      result = true;
      return result;
    }
    #endregion // Methods
  }
}
