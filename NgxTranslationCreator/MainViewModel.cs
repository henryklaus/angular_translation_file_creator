
using NgxTranslationCreator.Helper;
using log4net;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace NgxTranslationCreator
{
    public class MainViewModel : ViewModelBase
    {
        #region programm_variablen

        // private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog logger = LogManager.GetLogger(typeof(App));
        

        private Thread ProgressThread;

        #endregion

        #region View_variablen

        private bool _updateExisting = true;
        /// <summary>
        /// If true, updates existing translations files instead overwrite them
        /// </summary>
        public bool UpdateExisting
        {
            get { return _updateExisting; }
            set
            {
                _updateExisting = value;
                OnPropertyChanged();
                if (value == false && OnlyKeepExtractedTranslations == true)
                {
                    OnlyKeepExtractedTranslations = false;
                }
            }
        }

        private bool _onlyKeepExtractedTranslations = false;
        /// <summary>
        /// Deletes translations from the translations files, its keys couldn't find in project code
        /// Dependency on UpdateExisting: false, if UpdateExisting false
        /// </summary>
        public bool OnlyKeepExtractedTranslations
        {
            get { return _onlyKeepExtractedTranslations; }
            set
            {
                _onlyKeepExtractedTranslations = value;
                OnPropertyChanged();
            }
        }


        private string _searchDirectory;
        /// <summary>
        /// root-Directory, from where starts to scan your code deeply 
        /// </summary>
        public string SearchDirectory
        {
            get { return _searchDirectory; }
            set
            {
                _searchDirectory = value;
                OnPropertyChanged();
            }
        }

        private string _targetDirectory;
        /// <summary>
        /// Directory, to storage the generatet translation-files
        /// </summary>
        public string TargetDirectory
        {
            get { return _targetDirectory; }
            set
            {
                _targetDirectory = value;
                OnPropertyChanged();
            }
        }

        private Boolean _working;
        /// <summary>
        /// shows working state of Service
        /// </summary>
        public Boolean Working
        {
            get { return _working; }
            set
            {
                _working = value;
                OnPropertyChanged();
            }
        }

        private double _progressNumber = 0;
        /// <summary>
        /// shows Progress state of service (0-100 %)
        /// </summary>
        public double ProgressNumber
        {
            get { return _progressNumber; }
            set
            {
                _progressNumber = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region View_Commands



        private ICommand _selectSourceDirectory;

        public ICommand SelectSearchDirectory
        {
            get { return _selectSourceDirectory ?? (_selectSourceDirectory = new RelayCommand(OpenSearchDirectorySelectDialog)); }
        }


        private ICommand _selectTargetDirectory;

        public ICommand SelectTargetDirectory
        {
            get { return _selectTargetDirectory ?? (_selectTargetDirectory = new RelayCommand(OpenTargetDirectorySelectDialog)); }
        }


        private ICommand _startJob;

        public ICommand StartJob
        {
            get { return _startJob ?? (_startJob = new RelayCommand(StartWorkingThread)); }
        }

        private ICommand _stopJob;

        public ICommand StopJob
        {
            get { return _stopJob ?? (_stopJob = new RelayCommand(BreakWorkingThread)); }
        }
        #endregion

        #region constructors

        public MainViewModel() : base()
        {
        }

        #endregion

        #region view_methods

        private void UpdateProgressNumber(double number)
        {
            ProgressNumber = number;
        }


        private void OpenSearchDirectorySelectDialog(object obj)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    SearchDirectory = dialog.FileName;
                }
            }
        }

        private void OpenTargetDirectorySelectDialog(object obj)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    TargetDirectory = dialog.FileName;
                }
            }
        }

        private void BreakWorkingThread(object obj)
        {
            stopThread();
            Working = false;
        }

        private void StartWorkingThread(object obj)
        {
            Working = true;
            startThread();
        }

        private void ResetWorkingThread()
        {
            Working = false;
            ProgressThread = null;
        }
        #endregion

        #region thread

        private void startThread()
        {
            if(ProgressThread != null)
            {
                stopThread();
            }
            var process = new WorkingProcess(this.SearchDirectory, this.TargetDirectory, this.UpdateExisting, this.OnlyKeepExtractedTranslations, this.ResetWorkingThread, this.UpdateProgressNumber);
            ProgressThread = new Thread(process.Process);
            ProgressThread.Start();
        }

        private void stopThread()
        {
            if(ProgressThread != null && ProgressThread.IsAlive)
            {
                ProgressThread.Abort();
            }
            ProgressThread = null;
        }

       

        #endregion

    }
}
