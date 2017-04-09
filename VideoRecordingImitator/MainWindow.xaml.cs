using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.IO;

namespace VideoRecordingImitator
{
    public delegate void Reading(RealtimeVideoReader VideoPlayer);
    public delegate void Processing();

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int fstreamVDICount;
        FrameRate FPS = FrameRate.AUTO;

        JpegBuffer jpegBuffer = new JpegBuffer();
        const int MinBufferSize = 5;
        const int MaxBufferSize = 50; 

        /// <summary>
        /// Thread lock.
        /// </summary>
        System.Threading.Mutex c_threadLock;
        protected object m_threadLock = new object();

        bool StartClicked;

        int NumberOfThreads = 1;  //4
        System.Threading.Thread[] VideoPlayngThreads;
        System.Threading.Thread VideoPlayng;
        System.Threading.Thread VDBReaderSingle;
        List<Bitmap> BitmapPool = new List<Bitmap>(); //Буфер изображений

        public MainWindow()
        {
            InitializeComponent();

            jpegBuffer = new JpegBuffer(MaxBufferSize);
            jpegBuffer.SizeChangedEvent += new SizeChangedDel(BitmapCache_SizeChanged);
            this.bufferStateProgressBar.Maximum = MaxBufferSize;
            this.bufferStateProgressBar.Minimum = MinBufferSize;
            this.bufferStateProgressBar.Value = MinBufferSize;

        }

        //Кнопка выбора каталога проезда
        private void btOpenVideoArchive_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fileBrowser = new System.Windows.Forms.OpenFileDialog { Filter = "Video Database Index File (*.vdi)|*.vdi", Title = "Open Video Database Index File" })
            {
                if (fileBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ComboBoxItem Item = new ComboBoxItem();
                    Item.Content = fileBrowser.FileName;
                    this.cbVideoArchive.Text = fileBrowser.FileName;
                    this.cbVideoArchive.ApplyTemplate();
                    this.cbVideoArchive.InvalidateVisual();
                    this.cbVideoArchive.Items.Add(Item);
                    this.cbVideoArchive.SelectedItem = 0;
                    RealTimeInitialise(cbVideoArchive.Text);
                }
            }
        }

        private void btVideoArchiveSave_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog foderBrowser = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (foderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.tbSaveRecordedArchive.Text = foderBrowser.SelectedPath; 
                }
            }
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (!StartClicked)
            {
                if (VideoPlayng == null && VDBReaderSingle == null)
                    RealTimeInitialise(cbVideoArchive.Text);

                try
                {
                    if (VideoPlayng.ThreadState == System.Threading.ThreadState.Stopped || VideoPlayng.ThreadState == System.Threading.ThreadState.Unstarted)
                    {
                        fstreamVDICount = 0;
                        VideoPlayng.Start();
                    }
                    else
                    {
                        VideoPlayng.Resume();
                    }
                    StartClicked = true;
                    btStart.Content = "Пауза";
                }
                catch (NullReferenceException NE)
                {
                    if (VideoPlayng == null)
                    {
                        MessageBox.Show("Откройте файл");
                    }
                }
                catch (System.Threading.ThreadStateException ThreadStateExeption) { }
            }
            else
            {
                try
                {
                        VideoPlayng.Suspend();
                        StartClicked = false;
                        btStart.Content = "Старт";
                }
                catch (NullReferenceException NE) { }
            }
        }

        private void btFullStop_Click(object sender, RoutedEventArgs e)
        {
            if (!StartClicked)
            {
                try
                {
                    for (int i = 0; i < VideoPlayngThreads.Length; i++)
                    {
                        VideoPlayngThreads[i].Suspend();
                    }
                }
                catch (NullReferenceException NE) { }
            }
            jpegBuffer = null;
            VideoPlayngThreads = null;
            VDBReaderSingle = null; 
        }

        /// <summary>
        /// Обработчик события изменения размера буфера изображений
        /// </summary>
        public void BitmapCache_SizeChanged()
        {
            if (jpegBuffer.isFull)
            {
                VDBReaderSingle.Suspend();
            }
            else
            {
                if (VDBReaderSingle.ThreadState == System.Threading.ThreadState.Suspended)
                    VDBReaderSingle.Resume();
            }
        }

        //---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Инициализация операций чтения/записи при неизвестной длине файла, т.н чтение реального времени
        /// </summary>
        private void RealTimeInitialise(string CurrentFile)
        {
            fstreamVDICount = 0;
            RealtimeVideoReader VideoPlayer = new RealtimeVideoReader(CurrentFile, (int)VideoRecordingImitator.LabWagonType.SMDL);
            RealtimeVideoWriter VideoRecorder = new RealtimeVideoWriter(this.tbSaveRecordedArchive.Text, (int)VideoRecordingImitator.LabWagonType.SMDL);
            StartClicked = false;
            //Обнуление номера частей

            VideoPlayng = new System.Threading.Thread(delegate()
            {
                Write(VideoRecorder);
            });

            VDBReaderSingle = new System.Threading.Thread(delegate()
            {
                Read(VideoPlayer);   
            });
        }

       
        /// <summary>
        /// Обработчик события смены Ограничения Частоты кадров
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFrameRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbFrameRate.Text)
            {
                case "AUTO": FPS = FrameRate.FPS_24;
                    break;
                case "24": FPS = FrameRate.FPS_24;
                    break;
                case "25": FPS = FrameRate.FPS_25;
                    break;
                case "30": FPS = FrameRate.FPS_30;
                    break;
                case "50": FPS = FrameRate.FPS_50;
                    break;
            }
        }

        /// <summary>
        /// Операция чтения
        /// </summary>
        /// <param name="VideoPlayer">Считыватель видеокадров</param>
        private void Read(RealtimeVideoReader VideoPlayer)
        {
            int indexFrame = 0;      
            fstreamVDICount = VideoPlayer.CurrentPosition;
            byte[] Frame = null;
            do
            {                
                if (!jpegBuffer.isFull)
                    Frame = VideoPlayer.ReadFrameVDI(fstreamVDICount, FPS);

                if (jpegBuffer.Size > MinBufferSize && jpegBuffer.Size < MaxBufferSize)
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        this.tbLogOffsets.AppendText("Индекс: " + VideoPlayer.VDBIndex + "\n");
                        this.tbLogOffsets.ScrollToEnd();
                    }));


                if (Frame != null)
                    jpegBuffer.Push(new Frame(Frame, fstreamVDICount));

                if (jpegBuffer.Size > MinBufferSize && jpegBuffer.Size < MaxBufferSize)
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        if (bufferStateProgressBar.Value != jpegBuffer.Size) bufferStateProgressBar.Value = jpegBuffer.Size;
                    }));

                fstreamVDICount++;
            }
            while (fstreamVDICount < VideoPlayer.OffsetsCount);
            //while (Offset > 128);
        }

        /// <summary>
        /// Операция записи
        /// </summary>
        /// <param name="VideoRecorder">Рекордер видеокадров</param>
        private void Write(RealtimeVideoWriter VideoRecorder)
        {
            if (VDBReaderSingle.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                VDBReaderSingle.Start();
            }

            while (VDBReaderSingle.ThreadState != System.Threading.ThreadState.Stopped)
            {
                if (jpegBuffer.Size > MinBufferSize) VideoRecorder.RecordFrame(jpegBuffer.Pull(), FPS);
                if (jpegBuffer.Size > MinBufferSize && jpegBuffer.Size < MaxBufferSize)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        if (bufferStateProgressBar.Value != jpegBuffer.Size) bufferStateProgressBar.Value = jpegBuffer.Size;
                    }));
                }
            }
        }

        private void AlwaysTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void AlwaysTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }



        /// <summary>
        /// Загрузка параметров при загрузке формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\settings.xml"))
            {
                // загружаем данные из файла program.xml 
                using (Stream stream = new FileStream(Directory.GetCurrentDirectory() + "\\settings.xml", FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(iniSettings));

                    // в тут же созданную копию класса iniSettings под именем iniSet
                    iniSettings iniSet = (iniSettings)serializer.Deserialize(stream);

                    // и загружаем параметры из файла
                    if (Directory.Exists(iniSet.Destination))
                        tbSaveRecordedArchive.Text = iniSet.Destination;
                    foreach (string ListLastOpenedFile in iniSet.ListLastOpenedFiles)
                    {
                        this.cbVideoArchive.Items.Clear();
                        ComboBoxItem Item = new ComboBoxItem();
                        Item.Content = ListLastOpenedFile;
                        this.cbVideoArchive.ApplyTemplate();
                        this.cbVideoArchive.InvalidateVisual();
                        this.cbVideoArchive.Items.Add(Item);
                        this.cbVideoArchive.SelectedItem = 1;
                    }

                    this.AlwaysTopCheckBox.IsChecked = iniSet.TopMost;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // создаём копию класса iniSettings с именем iniSet
            iniSettings iniSet = new iniSettings();

            // записываем в переменные класса значения параметров
            //iniSet.Source = comboBox1.Text;
            iniSet.ListLastOpenedFiles = new string[this.cbVideoArchive.Items.Count];
            for (int i = 0; i < iniSet.ListLastOpenedFiles.Length; i++)
            {
                ComboBoxItem Item = (ComboBoxItem)this.cbVideoArchive.Items[i];
                iniSet.ListLastOpenedFiles[i] = (string)Item.Content;
                //this.cbVideoArchive.Items.CopyTo((string[])iniSet.ListLastOpenedFiles, 0);
            }
            iniSet.FrameRate = this.cbFrameRate.Text;
            iniSet.TopMost = this.AlwaysTopCheckBox.IsChecked.Value;

            // выкидываем класс iniSet целиком в файл program.xml
            using (Stream writer = new FileStream("settings.xml", FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(iniSettings));
                serializer.Serialize(writer, iniSet);
            }
        }
    }

    /// <summary>
    /// Класс выполняющий сохранение настроек
    /// </summary>
    public class iniSettings // имя выбрано просто для читаемости кода впоследствии
    {
        public string Destination;           // Директория куда пишется имитация
        public string[] ListLastOpenedFiles;    // Список видеозаписей открытых ранее
        public string FrameRate;             // Число кадров
        public bool TopMost;                 // Поверх других окон
    }
}
