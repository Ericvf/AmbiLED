using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace Ambiled.Core
{
    public interface IViewModel
    {
        ObservableCollection<string> Messages { get; }
        string Message { get; set; }
        int FPS { get; set; }
        bool IsConnectOnStart { get; }
        bool EnableCapture { get; set; }
        bool EnableCrop { get; set; }
        bool EnableGamma { get; set; }

        ImageSource Image { get; set; }
        bool MonitorOne { get; }
        bool MonitorTwo { get; }

        ObservableCollection<string> ComDevices { get; set; }
        string ComDevice { get; }
        int BaudRate { get; }

        int Rows { get; }
        int Columns { get; }
        void Save();

        bool EnableFixedFPS { get; set; }
        bool EnableAuto3d { get; set; }
        bool Is3DSBS { get; set; }
        bool Is3DOff { get; set; }
        bool Is3DOU { get; set; }

        bool EnablePostprocessing { get; set; }

        bool EnableSmoothing { get; set; }
        float Smoothing { get; set; }

        float RChannel { get; }
        float GChannel { get; }
        float BChannel { get; }

        float Hue { get; set; }
        float Brightness { get; set; }
        float Saturation { get; set; }

        bool IsRBG { get; }
        bool IsBGR { get; }
        bool Is2bit { get; }
        bool Is5bit { get; }
        bool Is8bit { get; }

        bool IsAmbilight { get; }
        bool IsBoxlight { get; }

        bool ShowPreview { get; }
    }

    [Export(typeof(IViewModel))]
    public class MainVM : INotifyPropertyChanged, IViewModel
    {
        #region Runtime properties
        [XmlIgnore]
        public string CaptureTitle
        {
            get { return _CaptureTitle; }
            set
            {
                if (_CaptureTitle != value)
                {
                    _CaptureTitle = value;
                    OnPropertyChanged(CaptureTitlePropertyName);
                }
            }
        }
        private string _CaptureTitle;
        public const string CaptureTitlePropertyName = "CaptureTitle";

        [XmlIgnore]
        public ImageSource Image
        {
            get { return _Image; }
            set
            {
                if (_Image != value)
                {
                    _Image = value;
                    OnPropertyChanged(ImagePropertyName);
                }
            }
        }
        private ImageSource _Image;
        public const string ImagePropertyName = "Image";

        [XmlIgnore]
        public ObservableCollection<string> Messages
        {
            set
            {
                this.messages = value;
                this.OnPropertyChanged("Messages");
            }
            get
            {
                return messages;
            }
        }
        private ObservableCollection<string> messages = new ObservableCollection<string>();

        [XmlIgnore]
        public ObservableCollection<string> ComDevices
        {
            set
            {
                this.comDevices = value;
                this.OnPropertyChanged("ComDevices");
            }
            get
            {
                return comDevices;
            }
        }
        private ObservableCollection<string> comDevices = new ObservableCollection<string>();

        [XmlIgnore]
        public int FPS
        {
            get { return _FPS; }
            set
            {
                if (_FPS != value)
                {
                    _FPS = value;
                    OnPropertyChanged(FramesPropertyName);
                }
            }
        }
        private int _FPS;
        public const string FramesPropertyName = "FPS";

        [XmlIgnore]
        public bool EnableController
        {
            get { return _EnableController; }
            set
            {
                if (_EnableController != value)
                {
                    _EnableController = value;
                    //OnEnableControllerChange();
                    OnPropertyChanged(EnableControllerPropertyName);
                }
            }
        }
        private bool _EnableController;
        public const string EnableControllerPropertyName = "EnableController";

        #endregion 

        #region Properties

        public bool MonitorOne
        {
            get { return _MonitorOne; }
            set
            {
                if (_MonitorOne != value)
                {
                    _MonitorOne = value;
                    _MonitorTwo = !value;
                    OnPropertyChanged(MonitorOnePropertyName);
                    OnPropertyChanged(MonitorTwoPropertyName);
                }
            }
        }
        private bool _MonitorOne;
        public const string MonitorOnePropertyName = "MonitorOne";

        public bool MonitorTwo
        {
            get { return _MonitorTwo; }
            set
            {
                if (_MonitorTwo != value)
                {
                    _MonitorTwo = value;
                    _MonitorOne = !value;
                    OnPropertyChanged(MonitorOnePropertyName);
                    OnPropertyChanged(MonitorTwoPropertyName);
                }
            }
        }
        private bool _MonitorTwo;
        public const string MonitorTwoPropertyName = "MonitorTwo";

        public string ComDevice
        {
            get { return _ComDevice; }
            set
            {
                if (_ComDevice != value)
                {
                    _ComDevice = value;
                    OnPropertyChanged(ComDevicePropertyName);
                }
            }
        }
        private string _ComDevice;
        public const string ComDevicePropertyName = "ComDevice";

        public bool IsConnectOnStart
        {
            get { return _IsConnectOnStart; }
            set
            {
                if (_IsConnectOnStart != value)
                {
                    _IsConnectOnStart = value;
                    OnPropertyChanged(IsConnectOnStartPropertyName);
                }
            }
        }
        private bool _IsConnectOnStart = false;
        public const string IsConnectOnStartPropertyName = "IsConnectOnStart";

        public int BaudRate
        {
            get { return _BaudRate; }
            set
            {
                if (_BaudRate != value)
                {
                    _BaudRate = value;
                    OnPropertyChanged(BaudRatePropertyName);
                }
            }
        }
        private int _BaudRate = 115200;
        public const string BaudRatePropertyName = "BaudRate";

        public int ColumnsGrid
        {
            get { return _ColumnsGrid; }
            set
            {
                if (_ColumnsGrid != value)
                {
                    _ColumnsGrid = value;
                    OnPropertyChanged(ColumnsGridPropertyName);
                }
            }
        }
        private int _ColumnsGrid;
        public const string ColumnsGridPropertyName = "ColumnsGrid";

        public int Columns
        {
            get { return _Columns; }
            set
            {
                if (_Columns != value)
                {
                    _Columns = value;
                    OnPropertyChanged(ColumnsPropertyName);
                }
            }
        }
        private int _Columns = 35;
        public const string ColumnsPropertyName = "Columns";

        public int Rows
        {
            get { return _Rows; }
            set
            {
                if (_Rows != value)
                {
                    _Rows = value;
                    OnPropertyChanged(RowsPropertyName);
                }
            }
        }
        private int _Rows = 20;
        public const string RowsPropertyName = "Rows";

        public int RowsGrid
        {
            get { return _RowsGrid; }
            set
            {
                if (_RowsGrid != value)
                {
                    _RowsGrid = value;
                    OnPropertyChanged(RowsGridPropertyName);
                }
            }
        }
        private int _RowsGrid;
        public const string RowsGridPropertyName = "RowsGrid";

        public bool EnableCapture
        {
            get { return _EnableCapture; }
            set
            {
                if (_EnableCapture != value)
                {
                    _EnableCapture = value;
                    //OnEnableCaptureChange();
                    OnPropertyChanged(EnableCapturePropertyName);
                }
            }
        }
        private bool _EnableCapture;
        public const string EnableCapturePropertyName = "EnableCapture";

        public bool EnablePostprocessing
        {
            get { return _EnablePostprocessing; }
            set
            {
                if (_EnablePostprocessing != value)
                {
                    _EnablePostprocessing = value;
                    OnPropertyChanged(EnablePostprocessingPropertyName);
                }
            }
        }
        private bool _EnablePostprocessing = true;
        public const string EnablePostprocessingPropertyName = "EnablePostprocessing";

        public float Brightness
        {
            get { return _Brightness; }
            set
            {
                if (_Brightness != value)
                {
                    _Brightness = value;
                    OnPropertyChanged(BrightnessPropertyName);
                }
            }
        }
        private float _Brightness = 1f;
        public const string BrightnessPropertyName = "Brightness";

        public float RChannel
        {
            get { return _RChannel; }
            set
            {
                if (_RChannel != value)
                {
                    _RChannel = value;
                    OnPropertyChanged(RChannelPropertyName);
                }
            }
        }
        private float _RChannel = 1f;
        public const string RChannelPropertyName = "RChannel";

        public float GChannel
        {
            get { return _GChannel; }
            set
            {
                if (_GChannel != value)
                {
                    _GChannel = value;
                    OnPropertyChanged(GChannelPropertyName);
                }
            }
        }
        private float _GChannel = 1f;
        public const string GChannelPropertyName = "GChannel";

        public float BChannel
        {
            get { return _BChannel; }
            set
            {
                if (_BChannel != value)
                {
                    _BChannel = value;
                    OnPropertyChanged(BChannelPropertyName);
                }
            }
        }
        private float _BChannel = 1f;
        public const string BChannelPropertyName = "BChannel";

        public float Hue
        {
            get { return _Hue; }
            set
            {
                if (_Hue != value)
                {
                    _Hue = value;
                    OnPropertyChanged(HuePropertyName);
                }
            }
        }
        private float _Hue = 1f;
        public const string HuePropertyName = "Hue";

        public float Saturation
        {
            get { return _Saturation; }
            set
            {
                if (_Saturation != value)
                {
                    _Saturation = value;
                    OnPropertyChanged(SaturationPropertyName);
                }
            }
        }
        private float _Saturation = 1f;
        public const string SaturationPropertyName = "Saturation";

        public bool IsAmbilight
        {
            get { return _IsAmbilight; }
            set
            {
                if (_IsAmbilight != value)
                {
                    _IsAmbilight = value;
                    OnPropertyChanged(IsAmbilightPropertyName);
                }
            }
        }
        private bool _IsAmbilight;
        public const string IsAmbilightPropertyName = "IsAmbilight";

        public bool IsBoxlight
        {
            get { return _IsBoxlight; }
            set
            {
                if (_IsBoxlight != value)
                {
                    _IsBoxlight = value;
                    OnPropertyChanged(IsBoxlightPropertyName);
                }
            }
        }
        private bool _IsBoxlight;
        public const string IsBoxlightPropertyName = "IsBoxlight";

        public bool EnableAuto3d
        {
            get { return _EnableAuto3d; }
            set
            {
                if (_EnableAuto3d != value)
                {
                    _EnableAuto3d = value;
                    OnPropertyChanged(EnableAuto3dPropertyName);
                }
            }
        }
        private bool _EnableAuto3d;
        public const string EnableAuto3dPropertyName = "EnableAuto3d";

        public bool Is3DOff
        {
            get { return _Is3DOff; }
            set
            {
                if (_Is3DOff != value)
                {
                    _Is3DOff = value;
                    OnPropertyChanged(Is3DOffPropertyName);
                }
            }
        }
        private bool _Is3DOff = true;
        public const string Is3DOffPropertyName = "Is3DOff";

        public bool Is3DSBS
        {
            get { return _Is3DSBS; }
            set
            {
                if (_Is3DSBS != value)
                {
                    _Is3DSBS = value;
                    OnPropertyChanged(Is3DSBSPropertyName);
                }
            }
        }
        private bool _Is3DSBS;
        public const string Is3DSBSPropertyName = "Is3DSBS";
        public bool Is3DOU
        {
            get { return _Is3DOU; }
            set
            {
                if (_Is3DOU != value)
                {
                    _Is3DOU = value;
                    OnPropertyChanged(Is3DOUPropertyName);
                }
            }
        }
        private bool _Is3DOU;
        public const string Is3DOUPropertyName = "Is3DOU";

        public bool EnableSmoothing
        {
            get { return _EnableSmoothing; }
            set
            {
                if (_EnableSmoothing != value)
                {
                    _EnableSmoothing = value;
                    OnPropertyChanged(EnableSmoothingPropertyName);
                }
            }
        }
        private bool _EnableSmoothing = true;
        public const string EnableSmoothingPropertyName = "EnableSmoothing";

        public float Smoothing
        {
            get { return _Smoothing; }
            set
            {
                if (_Smoothing != value)
                {
                    _Smoothing = value;
                    OnPropertyChanged(SmoothingPropertyName);
                }
            }
        }
        private float _Smoothing = 0.5f;
        public const string SmoothingPropertyName = "Smoothing";

        public bool EnableCrop
        {
            get { return _EnableCrop; }
            set
            {
                if (_EnableCrop != value)
                {
                    _EnableCrop = value;
                    OnPropertyChanged(EnableCropPropertyName);
                }
            }
        }
        private bool _EnableCrop;
        public const string EnableCropPropertyName = "EnableCrop";

        public int CropX
        {
            get { return _CropX; }
            set
            {
                if (_CropX != value)
                {
                    _CropX = value;
                    OnPropertyChanged(CropXPropertyName);
                }
            }
        }
        private int _CropX;
        public const string CropXPropertyName = "CropX";

        public int CropY
        {
            get { return _CropY; }
            set
            {
                if (_CropY != value)
                {
                    _CropY = value;
                    OnPropertyChanged(CropYPropertyName);
                }
            }
        }
        private int _CropY;
        public const string CropYPropertyName = "CropY";

        public int MaxCropX
        {
            get { return _MaxCropX; }
            set
            {
                if (_MaxCropX != value)
                {
                    _MaxCropX = value;
                    OnPropertyChanged(MaxCropXPropertyName);
                }
            }
        }
        private int _MaxCropX;
        public const string MaxCropXPropertyName = "MaxCropX";

        public int MaxCropY
        {
            get { return _MaxCropY; }
            set
            {
                if (_MaxCropY != value)
                {
                    _MaxCropY = value;
                    OnPropertyChanged(MaxCropYPropertyName);
                }
            }
        }
        private int _MaxCropY;
        public const string MaxCropYPropertyName = "MaxCropY";

        public bool EnableFixedFPS
        {
            get { return _EnableFixedFPS; }
            set
            {
                if (_EnableFixedFPS != value)
                {
                    _EnableFixedFPS = value;
                    OnPropertyChanged(EnableFixedFPSPropertyName);
                }
            }
        }
        private bool _EnableFixedFPS = true;
        public const string EnableFixedFPSPropertyName = "EnableFixedFPS";

        public bool Is2bit
        {
            get { return _Is2bit; }
            set
            {
                if (_Is2bit != value)
                {
                    _Is2bit = value;
                    OnPropertyChanged(Is2bitPropertyName);
                }
            }
        }
        private bool _Is2bit;
        public const string Is2bitPropertyName = "Is2bit";

        public bool Is5bit
        {
            get { return _Is5bit; }
            set
            {
                if (_Is5bit != value)
                {
                    _Is5bit = value;
                    OnPropertyChanged(Is5bitPropertyName);
                }
            }
        }
        private bool _Is5bit;
        public const string Is5bitPropertyName = "Is5bit";

        public bool Is8bit
        {
            get { return _Is8bit; }
            set
            {
                if (_Is8bit != value)
                {
                    _Is8bit = value;
                    OnPropertyChanged(Is8bitPropertyName);
                }
            }
        }
        private bool _Is8bit = true;
        public const string Is8bitPropertyName = "Is8bit";

        public bool Is12bit
        {
            get { return _Is12bit; }
            set
            {
                if (_Is12bit != value)
                {
                    _Is12bit = value;
                    OnPropertyChanged(Is12bitPropertyName);
                }
            }
        }
        private bool _Is12bit;
        public const string Is12bitPropertyName = "Is12bit";

        public bool IsRGB
        {
            get { return _IsRGB; }
            set
            {
                if (_IsRGB != value)
                {
                    _IsRGB = value;
                    OnPropertyChanged(IsRGBPropertyName);
                }
            }
        }
        private bool _IsRGB;
        public const string IsRGBPropertyName = "IsRGB";

        public bool IsBGR
        {
            get { return _IsBGR; }
            set
            {
                if (_IsBGR != value)
                {
                    _IsBGR = value;
                    OnPropertyChanged(IsBGRPropertyName);
                }
            }
        }
        private bool _IsBGR = true;
        public const string IsBGRPropertyName = "IsBGR";

        public bool IsRBG
        {
            get { return _IsRBG; }
            set
            {
                if (_IsRBG != value)
                {
                    _IsRBG = value;
                    OnPropertyChanged(IsRBGPropertyName);
                }
            }
        }
        private bool _IsRBG;
        public const string IsRBGPropertyName = "IsRBG";

        public bool EnableGamma
        {
            get { return _EnableGamma; }
            set
            {
                if (_EnableGamma != value)
                {
                    _EnableGamma = value;
                    OnPropertyChanged(EnableGammaPropertyName);
                }
            }
        }
        private bool _EnableGamma;
        public const string EnableGammaPropertyName = "EnableGamma";

        public float GammeValue
        {
            get { return _GammeValue; }
            set
            {
                if (_GammeValue != value)
                {
                    _GammeValue = value;
                    OnPropertyChanged(GammeValuePropertyName);
                }
            }
        }
        private float _GammeValue = 1.6f;
        public const string GammeValuePropertyName = "GammeValue";

        public string Message
        {
            get { return _Message; }
            set
            {
                if (_Message != value)
                {
                    _Message = value;
                    OnPropertyChanged(MessagePropertyName);
                }
            }
        }
        private string _Message;
        public const string MessagePropertyName = "Message";

        public bool ShowPreview
        {
            get { return _ShowPreview; }
            set
            {
                if (_ShowPreview != value)
                {
                    _ShowPreview = value;
                    if (!value)
                    {
                        Image = null;
                    }
                    OnPropertyChanged(ShowPreviewPropertyName);
                }
            }
        }
        private bool _ShowPreview;
        public const string ShowPreviewPropertyName = "ShowPreview";

        #endregion

        #region Loading
        public static MainVM LoadFile()
        {
            using (var myIsolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                var viewModel = default(MainVM);

                if (myIsolatedStorage.FileExists("MainVM.xml"))
                {
                    using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("MainVM.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(MainVM));
                        viewModel = (MainVM)serializer.Deserialize(stream);
                    }
                }
                else
                {
                    viewModel = new MainVM();
                }

                if (!viewModel.MonitorOne && !viewModel.MonitorTwo)
                    viewModel.MonitorOne = true;

                return viewModel;
            }
        }

        public void Save()
        {
            using (var isolatedStorageFile = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                var xmlWriterSettings = new XmlWriterSettings()
                {
                    Indent = true
                };

                var stream = isolatedStorageFile.OpenFile("MainVM.xml", FileMode.Create);
                var serializer = new XmlSerializer(typeof(MainVM));
                using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                {
                    serializer.Serialize(xmlWriter, this);
                }
            }
        }

        #endregion 

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
