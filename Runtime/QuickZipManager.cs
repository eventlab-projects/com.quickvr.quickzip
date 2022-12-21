using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Threading;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace QuickVR
{

    public static class QuickZipManager
    {

        #region GET AND SET

        /// <summary>
        /// Creates a zip file. 
        /// </summary>
        /// <param name="pathSrc"></param>
        /// <param name="pathDst"></param>
        public static void CreateZip(string pathSrc, string pathDst)
        {
            if (IsFilePath(pathSrc))
            {
                CreateZipFromFile(pathSrc, pathDst);
            }
            else
            {
                CreateZipFromDirectory(pathSrc, pathDst);
            }
        }

        /// <summary>
        /// Creates a zip file asynchronously. 
        /// </summary>
        /// <param name="pathSrc"></param>
        /// <param name="pathDst"></param>
        /// <returns></returns>
        public static WaitForThread CreateZipAsync(string pathSrc, string pathDst)
        {
            Thread thread = new Thread(()=> CreateZip(pathSrc, pathDst));
            return new WaitForThread(thread);
        }

        /// <summary>
        /// Creates a zip file from an array of bytes. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName"></param>
        /// <param name="pathDst"></param>
        public static void CreateZip(byte[] data, string entryName, string pathDst)
        {
            byte[] zipData = CreateZip(data, entryName);

            BinaryWriter bWriter = new BinaryWriter(File.OpenWrite(pathDst));
            bWriter.Write(zipData);
            bWriter.Close();
        }

        /// <summary>
        /// Creates a zip file from an array of bytes asyncrhonously. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName"></param>
        /// <param name="pathDst"></param>
        /// <returns></returns>
        public static WaitForThread CreateZipAsync(byte[] data, string entryName, string pathDst)
        {
            Thread thread = new Thread(() => CreateZip(data, entryName, pathDst));
            return new WaitForThread(thread);
        }

        /// <summary>
        /// Creates a zip byte array from the input data. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName"></param>
        /// <returns></returns>
        public static byte[] CreateZip(byte[] data, string entryName)
        {
            MemoryStream result = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(result);

            zipStream.SetLevel(3);

            ZipEntry newEntry = new ZipEntry(entryName);
            newEntry.DateTime = System.DateTime.Now;

            zipStream.PutNextEntry(newEntry);

            StreamUtils.Copy(new MemoryStream(data), zipStream, new byte[4096]);
            zipStream.CloseEntry();

            zipStream.IsStreamOwner = false;
            zipStream.Close();

            return result.ToArray();
        }

        /// <summary>
        /// Creates a zip byte array from the input data asynchronously. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName"></param>
        /// <returns></returns>
        public static ZipAsyncOperation CreateZipAsync(byte[] data, string entryName)
        {
            ZipAsyncOperation result = new ZipAsyncOperation();
            Thread thread = new Thread(() => CreateZipAsync(data, entryName, result));
            thread.Start();
            
            return result;
        }

        private static void CreateZipAsync(byte[] data, string entryName, ZipAsyncOperation result)
        {
            result._data = CreateZip(data, entryName);
            result._isDone = true;
        }

        /// <summary>
        /// Extracts the contents of a zip file. 
        /// </summary>
        /// <param name="zipPath"></param>
        public static void ExtractZip(string zipPath, string pathDst)
        {
            FastZip fZip = new FastZip();
            fZip.ExtractZip(zipPath, pathDst, null);
        }

        public static WaitForThread ExtractZipAsync(string zipPath, string pathDst)
        {
            Thread thread = new Thread(() => ExtractZip(zipPath, pathDst));
            return new WaitForThread(thread);
        }

        /// <summary>
        /// Extracts the contents of a byte array representing some zip compressed data. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] ExtractZip(byte[] data)
        {
            MemoryStream result = new MemoryStream();
            MemoryStream inputStream = new MemoryStream(data);
            ZipInputStream zipStream = new ZipInputStream(inputStream);

            zipStream.GetNextEntry();
            zipStream.CopyTo(result);

            return result.ToArray();
        }

        /// <summary>
        /// Extracts the contents of a byte array representing some zip compressed data assyncrhonously. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ZipAsyncOperation ExtractZipAsync(byte[] data)
        {
            ZipAsyncOperation result = new ZipAsyncOperation();
            Thread thread = new Thread(() => ExtractZipAsync(data, result));
            thread.Start();

            return result;
        }

        private static void ExtractZipAsync(byte[] data, ZipAsyncOperation result)
        {
            result._data = ExtractZip(data);
            result._isDone = true;
        }

        /// <summary>
        /// Creates a zip file from a single file. 
        /// </summary>
        /// <param name="pathSrc"></param>
        /// <param name="pathDst"></param>
        private static void CreateZipFromFile(string pathSrc, string pathDst)
        {
            //1) Create a tmp directory
            string fullPath = Path.GetFullPath(pathSrc);
            string fileName = Path.GetFileName(pathSrc);
            string pathTmp = fullPath.Replace(fileName, "") + Path.GetFileNameWithoutExtension(pathSrc);
            Directory.CreateDirectory(pathTmp);

            //2) Copy the file to the recently created directory
            File.Copy(pathSrc, Path.Combine(pathTmp, fileName));

            //3) Create a zip from the tmp directory
            CreateZipFromDirectory(pathTmp, pathDst);

            //4) Remove the tmp directory
            Directory.Delete(pathTmp, true);
        }

        /// <summary>
        /// Creates a zip file from a directory. 
        /// </summary>
        /// <param name="pathSrc"></param>
        /// <param name="pathDst"></param>
        private static void CreateZipFromDirectory(string pathSrc, string pathDst)
        {
            FastZip fZip = new FastZip();
            fZip.CreateZip(pathDst, pathSrc, true, null);
        }

        private static bool IsFilePath(string path)
        {
            return !IsDirectoryPath(path);
        }

        private static bool IsDirectoryPath(string path)
        {
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(path);

            //detect whether its a directory or file
            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }

        #endregion

    }

}

public class ZipAsyncOperation : CustomYieldInstruction
{

    #region PUBLIC ATTRIBUTES

    public override bool keepWaiting
    {
        get
        {
            return !_isDone;
        }
    }

    public bool _isDone = false;
    public byte[] _data = null;

    #endregion

}

public class WaitForThread : CustomYieldInstruction
{

    #region PUBLIC ATTRIUBTES

    public override bool keepWaiting
    {
        get
        {
            return _thread.IsAlive;
        }
    }

    #endregion

    #region PROTECTED ATTRIBUTES

    protected Thread _thread = null;

    #endregion

    #region CREATION AND DESTRUCTION

    public WaitForThread(Thread thread)
    {
        _thread = thread;
        _thread.Start();
    }

    #endregion

}




