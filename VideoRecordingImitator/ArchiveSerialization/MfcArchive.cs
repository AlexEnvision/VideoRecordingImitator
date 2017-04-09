using System;
using System.IO;
using System.Text;

namespace ArchiveSerialization
{
	public enum OleDateTimeStatus
	{
		Valid = 0,
		Invalid = 1,
		Null = 2
	};

	public enum OleCurrencyStatus
	{
		Valid = 0,
		Invalid = 1,
		Null = 2
	}

	/// <summary>
	/// Class allows reading objects serialize using MFC CArchive
	/// </summary>
	public class MfcArchive : Archive
	{
		public MfcArchive(Stream _stream, ArchiveOp _op)
			: base(_stream, _op)
		{
			if (_op == ArchiveOp.store)
			{
				throw new NotImplementedException("Writing to MFC compatible serialization is not supported.");
			}
		}

		new public void Read(out Decimal d)
		{
			// MFC stores decimal as 32-bit status value, 32-bit high value, and 32-bit low value
			Int32 status, high;
			UInt32 low;
			base.Read(out status);
			base.Read(out high);
			base.Read(out low);

			if (status != (int)OleCurrencyStatus.Valid)
			{
				d = 0;
			}
			else
			{
				Int64 final = MakeInt64((int)low, high);
				d = Decimal.FromOACurrency(final);
			}

		}

		new public void Read(out Boolean b)
		{
			// MFC stores bools as 32-bit "long"
			Int32 l;
			base.Read(out l);
			if (l == 0) b = false;
			else b = true;
		}


		new public void Read(out DateTime dt)
		{
			UInt32 status;
			base.Read(out status); // status is a 32-bit "long" in C++
			
			// MFC stores dates as 8-byte double
			Double l;
			base.Read(out l);
			dt = DateTime.FromOADate(l);

			if (status == (UInt32)OleDateTimeStatus.Null || 
				status == (UInt32)OleDateTimeStatus.Invalid)
			{
				// in this situation, the date is not valid.  
				// One option is to set to the initialized OLE Date value of 0.0
				dt = DateTime.FromOADate(0.0);
			}
		}

		public void Read(out DateTime? dt)
		{
			UInt32 status;
			base.Read(out status); // status is a 32-bit "long" in C++

			Double l;
			base.Read(out l);
			dt = DateTime.FromOADate(l);

			// read in nullable type
			if (status == (UInt32)OleDateTimeStatus.Null || 
				status == (UInt32)OleDateTimeStatus.Invalid)
			{
				dt = null;
			}
		}

		new public void Read(out string s)
		{
			s = MFCStringReader.ReadCString(this.reader);
		}
        new public void ReadUnicodeString(out string s)
        {
            s = base.reader.ReadString();
        }

		// Convert current low and high to 8-Byte C++ CURRENCY structure 
		static public Int64 MakeInt64(Int32 l1, Int32 l2)
		{
			return ((UInt32)(((UInt32)(l1)) | ((UInt32)((UInt32)(l2))) << 32));
		}

	} // end of class
}
