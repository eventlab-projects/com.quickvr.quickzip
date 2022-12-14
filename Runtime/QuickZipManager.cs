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
        public static ZipAsyncOperation CreateZipAsync(string pathSrc, string pathDst)
        {
            ZipAsyncOperation op = new ZipAsyncOperation();

            Thread thread = new Thread
            (
                () =>
                {
                    CreateZip(pathSrc, pathDst);
                    op._isDone = true;
                }
            );
            thread.Start();

            return op;
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
        public static ZipAsyncOperation CreateZipAsync(byte[] data, string entryName, string pathDst)
        {
            ZipAsyncOperation op = new ZipAsyncOperation();

            Thread thread = new Thread
            (
                () =>
                {
                    CreateZip(data, entryName, pathDst);
                    op._isDone = true;
                }
            );
            thread.Start();

            return op;
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
        public static ZipAsyncOperation<byte[]> CreateZipAsync(byte[] data, string entryName)
        {
            ZipAsyncOperation<byte[]> op = new ZipAsyncOperation<byte[]>();

            Thread thread = new Thread
            (
                () =>
                {
                    op._result = CreateZip(data, entryName);
                }
            );
            thread.Start();
            
            return op;
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

        public static ZipAsyncOperation ExtractZipAsync(string zipPath, string pathDst)
        {
            ZipAsyncOperation op = new ZipAsyncOperation();

            Thread thread = new Thread
            (
                () =>
                {
                    ExtractZip(zipPath, pathDst);
                    op._isDone = true;
                }
            );
            thread.Start();

            return op;
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
        public static ZipAsyncOperation<byte[]> ExtractZipAsync(byte[] data)
        {
            ZipAsyncOperation<byte[]> op = new ZipAsyncOperation<byte[]>();

            Thread thread = new Thread
            (
                () =>
                {
                    op._result = ExtractZip(data);
                }
            );
            thread.Start();

            return op;
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

public class ZipAsyncOperation<T> : ZipAsyncOperation
{

    #region PUBLIC ATTRIBUTES

    public T _result
    {
        get
        {
            return m_Result;
        }

        set
        {
            m_Result = value;
            _isDone = true;
        }
    }
    protected T m_Result;

    #endregion

}

public class ZipAsyncOperation : CustomYieldInstruction
{

    #region PUBLIC ATTRIBUTES

    public bool _isDone
    {
        get; set;
    }

    public override bool keepWaiting
    {
        get
        {
            return !_isDone;
        }
    }

    #endregion

}




