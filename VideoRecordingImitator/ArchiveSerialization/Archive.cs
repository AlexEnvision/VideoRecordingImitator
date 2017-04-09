using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace ArchiveSerialization
{
	public enum ArchiveOp
	{
		load = 0,
		store = 1
	}
	
	public class Archive
	{
		protected BinaryWriter writer = null;
		protected BinaryReader reader = null;
		protected ArchiveOp op = ArchiveOp.load;
		private const int m_Index = 0; // actually never changes
		
		public Archive(Stream _stream, ArchiveOp _op)
		{
			op = _op;
			
			if (_op == ArchiveOp.load)
			{
				reader = new BinaryReader(_stream);
			}
			else
			{
				writer = new BinaryWriter(_stream);
			}
		}

		public bool IsStoring()
		{
			if (op == ArchiveOp.store) return true;
			return false;
		}

		public void Serialize(IArchiveSerialization obj)
		{
			obj.Serialize(this);
		}

		//////////////////////////////////////////////////////
		// write functions

		public void Write(Char ch)
		{
			//writer.Write(ch);
			writer.Write(Convert.ToInt16(ch));
		}

		public void Write(UInt16 n)
		{
			writer.Write(n);
		}

		public void Write(Int16 n)
		{
			writer.Write(n);
		}

		public void Write(UInt32 n)
		{
			writer.Write(n);
		}

		public void Write(Int32 n)
		{
			writer.Write(n);
		}

		public void Write(UInt64 n)
		{
			writer.Write(n);
		}

		public void Write(Int64 n)
		{
			writer.Write(n);
		}

		public void Write(Single d)
		{
			writer.Write(d);
		}

		public void Write(Double d)
		{
			writer.Write(d);
		}

		public void Write(Decimal d)
		{
			// store decimals as Int64
			Int64 n = Decimal.ToOACurrency(d);
			writer.Write(n);
		}

		public void Write(DateTime dt)
		{
			writer.Write(dt.ToBinary());
		}

		public void Write(Boolean b)
		{
			writer.Write(b);
		}

		public void Write(string s)
		{
			writer.Write(Convert.ToInt32(s.Length));
			writer.Write(s.ToCharArray());
		}

		public void Write(Guid guid)
		{
			byte[] bytes = guid.ToByteArray();
			Write(bytes);
		}

		public void Write(Byte[] buffer)
		{
			writer.Write(buffer);
		}

		///////////////////////////////////////////////////
		// Read functions

		public void Read(out string s)
		{
			Int32 length = 0;
			Read(out length);

			char[] ch = new char[length];

			reader.Read(ch, m_Index, length);

			StringBuilder sb = new StringBuilder();
			sb.Append(ch);
			s = sb.ToString();
		}

		public void Read(out UInt16 n)
		{
			byte[] bytes = new byte[2];
			reader.Read(bytes, m_Index, 2);
			n = BitConverter.ToUInt16(bytes, 0);
		}

		public void Read(out Int16 n)
		{
			byte[] bytes = new byte[2];
			reader.Read(bytes, m_Index, 2);
			n = BitConverter.ToInt16(bytes, 0);
		}

		public void Read(out UInt32 n)
		{
			byte[] bytes = new byte[4];
			reader.Read(bytes, m_Index, 4);
			n = BitConverter.ToUInt32(bytes, 0);
		}

		public void Read(out Int32 n)
		{
			byte[] bytes = new byte[4];
			reader.Read(bytes, m_Index, 4);
			n = BitConverter.ToInt32(bytes, 0);
		}

		public void Read(out UInt64 n)
		{
			byte[] bytes = new byte[8];
			reader.Read(bytes, m_Index, 8);
			n = BitConverter.ToUInt64(bytes, 0);
		}

		public void Read(out Int64 n)
		{
			byte[] bytes = new byte[8];
			reader.Read(bytes, m_Index, 8);
			n = BitConverter.ToInt64(bytes, 0);
		}

		public void Read(out Char ch)
		{
			Int16 n;
			Read(out n);
			ch = Convert.ToChar(n);
			
			/* direct reading as char doesn't work for some reason
				Sometimes it works, but sometimes the character
			  takes up only one byte in the buffer and it seems
			  to depend on what comes before and after the item in the buffer
		 
			*/

//			byte[] bytes = new byte[2];
//			reader.Read(bytes, m_Index, 2);
//			ch = BitConverter.ToChar(bytes, 0);
		}

		public void Read(out float d)
		{
			byte[] bytes = new byte[4];
			reader.Read(bytes, m_Index, 4);
			d = BitConverter.ToSingle(bytes, 0);
		}

		public void Read(out double d)
		{
			byte[] bytes = new byte[8];
			reader.Read(bytes, m_Index, 8);
			d = BitConverter.ToDouble(bytes, 0);
		}

		public void Read(out Decimal d)
		{
			byte[] bytes = new byte[8];
			reader.Read(bytes, m_Index, 8);

			// BitConverter does not support direct conversion to Decimal so use Int64
			Int64 n = BitConverter.ToInt64(bytes, 0);
			d = Decimal.FromOACurrency(n);
		}

		public void Read(out DateTime dt)
		{
			Int64 l;
			Read(out l);
			dt = DateTime.FromBinary(l);
		}

		public void Read(out Boolean b)
		{
			byte[] bytes = new byte[1];
			reader.Read(bytes, m_Index, 1);
			b = BitConverter.ToBoolean(bytes, 0);
		}

		public void Read(out Guid guid)
		{
			byte[] bytes = new byte[16];
			Read(out bytes, 16);
			guid = new Guid(bytes);
		}

		public void Read(out byte[] buffer, int bufferSize)
		{
			buffer = new byte[bufferSize];
			reader.Read(buffer, m_Index, bufferSize);
		}
	} // end of class
} // end of namespace
