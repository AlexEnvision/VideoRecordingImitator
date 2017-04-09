using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace VideoRecordingImitator
{
    public enum LabWagonType
    {
        SMDL = 1,
        AGE001 = 2,
        AGE002 = 3,
        ChS200 = 4,
    }

    public class RealtimeVideoReader
    {
        public int CurrentPosition;
        List<int> Offsets;
        List<int> FrameLengths;
        int LabWagonType;

        const int FrameStride = 30;         //Число кадров, на которое текущая позиция чтения сдвигается назад

        string CurrentVDBFileName;
        string CurrentVDIFileName;
        int NumberOfPartition;
        int NumberOfThreads = 0;

        public int OffsetsCount
        {
            get { return Offsets.Count; }
        }

        //----Подстроить под это значение------
        int vdbIndex = 640;
        public int VDBIndex
        {
            get { return vdbIndex; }
        }
        //--------------------------------------

        //Смещения в VDI файле
        int offsetB = 522,  //640 
            offsetE = 540;  //640
        int offsetBDB = 0,
            offsetBDE = 0;

        public RealtimeVideoReader(string vdiFile, int LabWagonType)
        {
            if (LabWagonType != 0)
                this.LabWagonType = (int)VideoRecordingImitator.LabWagonType.SMDL;
            else
                this.LabWagonType = LabWagonType;

            this.CurrentPosition = 0;
            this.NumberOfPartition = 0;
            Offsets = new List<int>();
            FrameLengths = new List<int>();

            this.CurrentVDIFileName = VDISelector(vdiFile);
            this.CurrentVDBFileName = CurrentVDIFileName.Substring(0, CurrentVDIFileName.Length - 4) + "_00" + ".vdb";
            ToCurrentPosition(CurrentVDIFileName);           
        }

        /// <summary>
        /// Выбирает VDI файл проезда
        /// </summary>
        private string VDISelector(string VdiFile)
        {
            //string[] VDIFiles = Directory.GetFiles(VdiFile + "\\", "*_c2.vdi");
            //int CurrentVDIFile = 0;
            string CurrentVDIFileName = VdiFile;
      
            return CurrentVDIFileName;
        }

        /// <summary>
        /// Переход к текущей позиции записи
        /// </summary>
        public void ToCurrentPosition(string CurrendVdiFile)
        {
            this.CurrentVDIFileName = VDISelector(CurrendVdiFile);
            this.CurrentVDBFileName = CurrentVDIFileName.Substring(0, CurrentVDIFileName.Length - 4) + "_00" + ".vdb";
            ReadFullVDI(CurrentVDIFileName);
            if (Offsets != null)
            {
                this.CurrentPosition = Offsets.Count - FrameStride;
            }
        }

        /// <summary>
        /// Получение смещения из индексного файла vdi
        /// </summary>
        /// <param name="CurrentPosition"></param>
        /// <returns></returns>
        public byte[] ReadFrameVDI(int CurrentPosition, FrameRate FPS)
        {
            System.Threading.Thread.Sleep((int)FPS);
            this.CurrentPosition = CurrentPosition;

            string p = this.CurrentVDIFileName;
            using (FileStream fstreamVDI = File.OpenRead(p))
            {
                fstreamVDI.Seek(offsetB, SeekOrigin.Begin);
                byte[] array = new byte[4];
                fstreamVDI.Read(array, 0, array.Length);
                //array.Reverse();
                offsetBDB = BitConverter.ToInt32(array, 0) + 128;

                fstreamVDI.Seek(offsetE, SeekOrigin.Begin);
                array = new byte[4];
                fstreamVDI.Read(array, 0, array.Length);
                array.Reverse();
                offsetBDE = BitConverter.ToInt32(array, 0) + 128;

                Offsets.Add(offsetBDB);
                FrameLengths.Add(offsetBDE - offsetBDB);

                offsetB += 18;
                offsetE += 18;
            }

            this.vdbIndex = Offsets[CurrentPosition];
            return ReadFrameVDB(CurrentPosition, Offsets[CurrentPosition], FrameLengths[CurrentPosition]);
        }

        private void ReadFullVDI(string VDIFileName)
        {
            int offsetB = 522,  //640 
                offsetE = 540;  //640
            int offsetBDB = 0,
                offsetBDE = 0;
            List<int> foffset = new List<int>();
            List<int> flength = new List<int>();

            string p = VDIFileName;
            using (FileStream fstreamVDI = File.OpenRead(p))
            {
                while (offsetE < fstreamVDI.Length - (18 * FrameStride))
                {
                    fstreamVDI.Seek(offsetB, SeekOrigin.Begin);
                    byte[] array = new byte[4];
                    fstreamVDI.Read(array, 0, array.Length);
                    //array.Reverse();
                    offsetBDB = BitConverter.ToInt32(array, 0) + 128;

                    fstreamVDI.Seek(offsetE, SeekOrigin.Begin);
                    array = new byte[4];
                    fstreamVDI.Read(array, 0, array.Length);
                    array.Reverse();
                    offsetBDE = BitConverter.ToInt32(array, 0) + 128;

                    foffset.Add(offsetBDB);
                    flength.Add(offsetBDE - offsetBDB);

                    offsetB += 18;
                    offsetE += 18;
                }
                Offsets = foffset;
                FrameLengths = flength;

                //this.offsetB = offsetB;
                //this.offsetE = offsetE;

                //NumberOfPartition = 0;
                //for (int i = 0; i < FrameLengths.Count; i++)
                //    if (FrameLengths[i] < 0) NumberOfPartition++;
            
            }
        }

        /// <summary>
        /// Получение кадра VDB файла
        /// </summary>
        /// <param name="position"></param>
        /// <param name="offset"></param>
        /// <param name="FrameLength"></param>
        /// <returns></returns>
        public byte[] ReadFrameVDB(int position, int offset, int FrameLength)
        {
            byte[] VDBFrame = null;
            bool findPointTransit = false;
            for (int i = 0; i < NumberOfThreads; i++)
            {
                if (FrameLengths[position + i] < 0)
                {
                    findPointTransit = true;
                }
            }

            if (findPointTransit)  //Организация перехода с одного на другой файл
            {
                NumberOfPartition++;
                if (NumberOfPartition < 10)
                    CurrentVDBFileName = CurrentVDBFileName.Substring(0, CurrentVDBFileName.Length - 7) + "_0" + NumberOfPartition + ".vdb";
                if (NumberOfPartition >= 10)
                    CurrentVDBFileName = CurrentVDBFileName.Substring(0, CurrentVDBFileName.Length - 7) + "_" + NumberOfPartition + ".vdb";
                position += NumberOfThreads + 0;
            }
            else
            {
                VDBFrame = GetFrameByte(offset, FrameLength);
            }
            return VDBFrame;
        }

        /// <summary>
        /// Получение кадра
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="FrameLength"></param>
        /// <returns></returns>
        private Bitmap GetFrame(int offset, int FrameLength)
        {
            Bitmap sourceData = null;
            using (FileStream fstream = File.OpenRead(CurrentVDBFileName))
            {
                try
                {
                    fstream.Seek(offset, SeekOrigin.Begin);
                    byte[] array = new byte[FrameLength];      // - length = -1073516820                           
                    fstream.Read(array, 0, array.Length);

                    using (MemoryStream mstream = new MemoryStream())
                    {
                        // преобразуем строку в байты
                        byte[] array2 = array;
                        // запись массива байтов в файл
                        mstream.Write(array2, 0, array2.Length);
                        sourceData = (Bitmap)Bitmap.FromStream(mstream);
                    }
                }
                catch (OverflowException) { }
                catch (ArgumentException AE) { }
            }
            return sourceData;
        }

        /// <summary>
        /// Получение кадра в виде байтов
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="FrameLength"></param>
        /// <returns></returns>
        private byte[] GetFrameByte(int offset, int FrameLength)
        {
            byte[] sourceData = null;
            using (FileStream fstream = File.OpenRead(CurrentVDBFileName))
            {
                try
                {
                    fstream.Seek(offset, SeekOrigin.Begin);
                    byte[] array = new byte[FrameLength];      // - length = -1073516820                           
                    fstream.Read(array, 0, array.Length);

                    sourceData = array;
                }
                catch (OverflowException) { }
                catch (ArgumentException AE) { }
            }
            return sourceData;
        }
    }
}
