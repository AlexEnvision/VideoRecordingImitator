using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace VideoRecordingImitator
{
    //пользовательский делегат  
    public delegate void SizeChangedDel(); 

    public class JpegBuffer
    { 
        //событие  
        public event SizeChangedDel SizeChangedEvent;

        private List<Frame> Images = new List<Frame>();
        private int size;
        private bool full;

        public int Delay { get; set; }
        public bool isFull
        { 
            get { return full; }
        }
        public int Size 
        { 
            get { return size; } 
        }

        public JpegBuffer() { }
        public JpegBuffer(int Delay)
        { 
            this.Delay = Delay;
            full = false;
        }

        public void Push(Frame FrameBitmap)
        {
            if (Images.Count < Delay)
            {
                Images.Add(FrameBitmap);
                size++;
            }
            else
            {
                full = true;
                //генерирование события  
                if (SizeChangedEvent != null)
                    SizeChangedEvent();
            }
        }

        public byte[] Pull(int Index)
        {
            Frame temp = null;
            if (Images[0].Index == Index && Images.Count != 0)
            {
                temp = Images[0];
                Images.RemoveAt(0);
                size--;
                full = false;
                //генерирование события  
                if (SizeChangedEvent != null)
                    SizeChangedEvent();
            }
            return temp.Store;
        }
        public byte[] Pull()
        {
            Frame temp = Images[0];
            Images.RemoveAt(0);
            size--;
            full = false;
            //генерирование события  
            if (SizeChangedEvent != null)
                SizeChangedEvent();
            return temp.Store;
        }

        public void Clear()
        {
            Images.Clear();
        }

        public void Dispose()
        {
 
        }
    }

    public class Frame
    {
        public  int Index { get; set; }
        public byte[] Store { get; set; }

        public Frame(byte[] Source, int Index)
        {
            this.Store = Source;
            this.Index = Index;
        }
    }
}



//int Width 
//{
//    get {return Store.Width; }
//}
//int Height
//{
//    get { return Store.Height; }
//}
//Size Size
//{
//    get { return Store.Size; }
//}

//public Bitmap this[int index]
//{
//    get
//    {
//        return Images[index];
//    }
//    set
//    {
//        Images[index] = value;
//    }
//}