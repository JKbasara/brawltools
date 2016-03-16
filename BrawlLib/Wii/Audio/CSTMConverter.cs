﻿using System;
#if RSTMLIB
#else
using BrawlLib.IO;
#endif
using System.Audio;
using BrawlLib.SSBBTypes;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BrawlLib.Wii.Audio
{
    public static class CSTMConverter
    {
        internal static unsafe byte[] FromRSTM(RSTMHeader* rstm)
        {
            StrmDataInfo strmDataInfo = *rstm->HEADData->Part1;
            int channels = strmDataInfo._format._channels;

            // Get section sizes from the BRSTM - BCSTM is such a similar format that we can assume the sizes will match.
            int rstmSize = 0x40;
            int infoSize = rstm->_headLength;
            int seekSize = rstm->_adpcLength;
            int dataSize = rstm->_dataLength;

            //Create byte array
            byte[] array = new byte[rstmSize + infoSize + seekSize + dataSize];

            fixed (byte* address = array) {
                //Get section pointers
                CSTMHeader* cstm = (CSTMHeader*)address;
                CSTMINFOHeader* info = (CSTMINFOHeader*)((byte*)cstm + rstmSize);
                CSTMSEEKHeader* seek = (CSTMSEEKHeader*)((byte*)info + infoSize);
                CSTMDATAHeader* data = (CSTMDATAHeader*)((byte*)seek + seekSize);

                //Initialize sections
                cstm->Set(infoSize, seekSize, dataSize);
                info->Set(infoSize, channels);
                seek->Set(seekSize);
                data->Set(rstm->DATAData->_length);

                //Set HEAD data
                info->_dataInfo = new CSTMDataInfo(strmDataInfo);

                //Create one ADPCMInfo for each channel
                IntPtr* adpcData = stackalloc IntPtr[channels];
                CSTMADPCMInfo** pAdpcm = (CSTMADPCMInfo**)adpcData;
                for (int i = 0; i < channels; i++)
                    *(pAdpcm[i] = info->GetChannelInfo(i)) = new CSTMADPCMInfo(*rstm->HEADData->GetChannelInfo(i));

                bshort* seekFrom = (bshort*)rstm->ADPCData->Data;
                short* seekTo = (short*)seek->Data;
                for (int i = 0; i < seek->_length / 2 - 8; i++)
                {
                    *(seekTo++) = *(seekFrom++);
                }

                VoidPtr dataFrom = rstm->DATAData->Data;
                VoidPtr dataTo = data->Data;
                Memory.Move(dataTo, dataFrom, (uint)data->_length - 8);
            }
            return array;
        }

        internal static unsafe byte[] ToRSTM(CSTMHeader* cstm)
        {
            CSTMDataInfo cstmDataInfo = cstm->INFOData->_dataInfo;
            int channels = cstmDataInfo._format._channels;

            // Get section sizes from the BRSTM - BCSTM is such a similar format that we can assume the sizes will match.
            int rstmSize = 0x40;
            int infoSize = cstm->_infoBlockSize;
            int seekSize = cstm->_seekBlockSize;
            int dataSize = cstm->_dataBlockSize;

            //Create byte array
            byte[] array = new byte[rstmSize + infoSize + seekSize + dataSize];

            fixed (byte* address = array)
            {
                //Get section pointers
                RSTMHeader* rstm = (RSTMHeader*)address;
                HEADHeader* info = (HEADHeader*)((byte*)rstm + rstmSize);
                ADPCHeader* seek = (ADPCHeader*)((byte*)info + infoSize);
                RSTMDATAHeader* data = (RSTMDATAHeader*)((byte*)seek + seekSize);

                //Initialize sections
                rstm->Set(infoSize, seekSize, dataSize);
                info->Set(infoSize, channels);
                seek->Set(seekSize);
                data->Set(cstm->DATAData->_length);

                //Set HEAD data
                *info->Part1 = new StrmDataInfo(cstmDataInfo, rstmSize + infoSize + seekSize + 0x20);

                //Create one ADPCMInfo for each channel
                IntPtr* adpcData = stackalloc IntPtr[channels];
                ADPCMInfo** pAdpcm = (ADPCMInfo**)adpcData;
                for (int i = 0; i < channels; i++)
                    *(pAdpcm[i] = info->GetChannelInfo(i)) = new ADPCMInfo(*cstm->INFOData->GetChannelInfo(i));

                bshort* seekFrom = (bshort*)cstm->SEEKData->Data;
                short* seekTo = (short*)seek->Data;
                for (int i = 0; i < seek->_length / 2 - 8; i++)
                {
                    *(seekTo++) = *(seekFrom++);
                }

                VoidPtr dataFrom = cstm->DATAData->Data;
                VoidPtr dataTo = data->Data;
                Memory.Move(dataTo, dataFrom, (uint)data->_length - 8);
            }
            return array;
        }

#if RSTMLIB
        public static unsafe byte[] FromRSTM(byte[] rstm)
        {
            fixed (byte* ptr = rstm)
            {
                return FromRSTM((RSTMHeader*)ptr);
            }
        }

        public static unsafe byte[] ToRSTM(byte[] cstm)
        {
            fixed (byte* ptr = cstm)
            {
                return ToRSTM((CSTMHeader*)ptr);
            }
        }

        public static unsafe byte[] EncodeToByteArray(IAudioStream stream, IProgressTracker progress)
        {
            return FromRSTM(RSTMConverter.EncodeToByteArray(stream, progress));
        }
#endif
    }
}