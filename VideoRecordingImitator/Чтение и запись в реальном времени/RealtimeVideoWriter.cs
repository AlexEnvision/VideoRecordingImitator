using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ArchiveSerialization;
using System.Runtime.InteropServices;

namespace VideoRecordingImitator
{
    class RealtimeVideoWriter
    {
        VDI Vdi = new VDI();
        VDI Vdb = new VDI();

        string RecordVDI;
        string RecordVDB;
        List<int> Offsets;
        List<int> OffsetsVDI;
        int CurrentPosition;
        int CurrentVDIFile;
        int NumberOfPartition;
        ArchiveSerialization.Archive ArVDB;
        ArchiveSerialization.Archive ArVDI;

        public RealtimeVideoWriter(string RecordPath, int LabWagonType)
        {
            CurrentPosition = 0;            
            Offsets = new List<int>();
            OffsetsVDI = new List<int>();
            CurrentVDIFile = 1;
            RecordVDI = GenerateVDIName(RecordPath, 1);
            RecordVDB = GenerateVDBName(RecordVDI, 0, 0);
            
        }

        private string GenerateVDIName(string RecordPath, int CurrentVDIIndex)
        {
            string RecordVDI = "";
            if (CurrentVDIFile < 10)
                RecordVDI = RecordPath + "\\" + "000" + CurrentVDIFile + "_c2" + ".vdi";
            if (CurrentVDIFile > 10 && CurrentVDIFile < 100)
                RecordVDI = RecordPath + "\\" + "00" + CurrentVDIFile + "_c2" + ".vdi";
            if (CurrentVDIFile > 100 && CurrentVDIFile < 1000)
                RecordVDI = RecordPath + "\\" + "0" + CurrentVDIFile + "_c2" + ".vdi";
            if (CurrentVDIFile > 1000)
                RecordVDI = RecordPath + "\\" + CurrentVDIFile + "_c2" + ".vdi";

            return RecordVDI;
        }

        private string GenerateVDBName(string RecordVDI, int NumberOfPartition, int CurrentVDIIndex)
        {
            string RecordVDB = "";
            if (NumberOfPartition < 10)
                RecordVDB = RecordVDI.Substring(0, RecordVDI.Length - 4) + "_0" + NumberOfPartition + ".vdb";
            if (NumberOfPartition > 10 && CurrentVDIFile < 100)
                RecordVDB = RecordVDI.Substring(0, RecordVDI.Length - 4) + "_" +NumberOfPartition + ".vdb";

            return RecordVDB;
        }


        private void CreateFile(string Name, TypeofFile typeData)
        {
            File.Create(Name);
            System.Threading.Thread.Sleep(500); 
            if (typeData == TypeofFile.VDB)
            {
                using (FileStream fileStreamVDB = File.OpenWrite(RecordVDB))
                {
                    ArVDB = new ArchiveSerialization.Archive(fileStreamVDB, ArchiveSerialization.ArchiveOp.store);
                    CreateVDBHandle(ArVDB);
                }
            }        
        }

        public void RecordFrame(byte[] jpeg, FrameRate FPS)
        {
            if (!File.Exists(RecordVDB))
            {
                CreateFile(RecordVDB, TypeofFile.VDB);
                if (!File.Exists(RecordVDI)) 
                     CreateFile(RecordVDI, TypeofFile.VDI);                             
            }

            try
            {               
                using (FileStream fileStreamVDB = File.OpenWrite(RecordVDB))
                {
                   
                    if (Offsets.Count == 0) fileStreamVDB.Position = 0;
                    else fileStreamVDB.Position = Offsets[CurrentPosition];
                    ArVDB = new ArchiveSerialization.Archive(fileStreamVDB, ArchiveSerialization.ArchiveOp.store);
                    VDBSerialize(jpeg, ArVDB, fileStreamVDB.Length);
                   
                    if (fileStreamVDB.Length > 1074279092)                    
                    {
                        this.NumberOfPartition++;
                        RecordVDB = GenerateVDBName(RecordVDI, NumberOfPartition, 1);
                        Offsets[CurrentPosition] = 0;
                    }
                }
            }
            catch { }

            try
            {              
                using (FileStream fileStreamVDI = File.OpenWrite(RecordVDI))
                {
                    if (OffsetsVDI.Count == 0) fileStreamVDI.Position = 0;
                    else fileStreamVDI.Position = OffsetsVDI[CurrentPosition - 1];
                    ArVDI = new ArchiveSerialization.Archive(fileStreamVDI, ArchiveSerialization.ArchiveOp.store);
                    VDISerialize(ArVDI, fileStreamVDI.Length);
                }
            }
            catch { }

            System.Threading.Thread.Sleep((int)FPS);
        }

        private void CreateVDBHandle(Archive ar)
        {
            if (ar.IsStoring())
            {
                ar.Write(Vdb.ID);
                ar.Write(Vdb.NumberRailDB);
                ar.Write(Vdb.LabWagonType);

                byte[] Reserved = new byte[377];
                ar.Write(Reserved);

                Offsets.Add(640);
            }
        }

        /// <summary>
        /// Сериализация VDI файла
        /// </summary>
        /// <param name="ar"></param>
        /// <param name="Length"></param>
        private void VDISerialize(Archive ar, long Length)
        {
            if (ar.IsStoring())
            {
                // Если файл только что создан, то его длина 0
                if (Length == 0)
                {
                    ar.Write(Vdi.ID);
                    ar.Write(Vdi.NumberRailDB);
                    ar.Write(Vdi.LabWagonType);

                    byte[] Reserved = new byte[515];
                    ar.Write(Reserved);

                    OffsetsVDI.Add(522);
                    ar.Write(Offsets[CurrentPosition]);
                }
                else
                {
                    ar.Write(Offsets[CurrentPosition]);
                    byte[] Reserved = new byte[18];
                    ar.Write(Vdi.Reserved);

                    int prevOffset = OffsetsVDI[OffsetsVDI.Count - 1];             
                    OffsetsVDI.Add(prevOffset + 0x12);                       
                }
            }         
        }

        private void VDBSerialize(byte[] jpeg, Archive ar, long Length)
        {
            if (ar.IsStoring())
            {
                // Если файл только что создан, то его длина 0
                //if (Length == 0)
                //{
                //    ar.Write(Vdb.ID);
                //    ar.Write(Vdb.NumberRailDB);
                //    ar.Write(Vdb.LabWagonType);

                //    byte[] Reserved = new byte[377];
                //    ar.Write(Reserved);

                //    Offsets.Add(640);
                //}
                //else
                //{
                byte[] Reserved = new byte[128];
                ar.Write(Reserved);

                byte[] array = jpeg;
                ar.Write(array);

                int prevOffset = Offsets[Offsets.Count - 1];              
                Offsets.Add(prevOffset + array.Length + 128);
                if (Offsets[CurrentPosition] > 1073741824)
                { Offsets[CurrentPosition] = 640; }

                CurrentPosition++;
                //}             
            }
        }        
    }

    /// <summary>
    /// Структура VDI Файла
    /// </summary>
    public class VDI
    {
        public byte[] ID = new byte[] { 0x56, 0x44, 0x42, 0 }; //ID - VDB
        public byte NumberRailDB = 96;                         // Задает номер региона ЖД    
        public short LabWagonType = 1447;                      // Задается номер вагона

        public byte Reserved = 0;                              // Зарезервировано             
    }

    /// <summary>
    /// Структура VDB файла
    /// </summary>
    public class VDB
    {
        // Создание заголовка (шапки) файла
        public byte[] ID = new byte[] { 0x56, 0x44, 0x42, 0 }; //ID - VDB
        public byte NumberRailDB = 96;                         // Задает номер региона ЖД    
        public short LabWagonType = 1447;                      // Задается номер вагона

        public byte Reserved = 0;                              // Зарезервировано 

    }

    public enum TypeofFile
    {
        VDI = 0,
        VDB = 1,
    }
}
