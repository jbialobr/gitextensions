using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitUI;
using Microsoft.VisualStudio.Threading;

namespace GitCommands
{
    public class SynchronizedProcessReader
    {
        public Process Process { get; }
        public byte[] Output { get; private set; }
        public byte[] Error { get; private set; }

        private readonly JoinableTask _stdOutputLoaderThread;
        private readonly JoinableTask _stdErrLoaderThread;

        public SynchronizedProcessReader(Process process)
        {
            Process = process;
            _stdOutputLoaderThread = ThreadHelper.JoinableTaskFactory.RunAsync(async () => Output = await ReadByteAsync(Process.StandardOutput.BaseStream).ConfigureAwait(false));
            _stdErrLoaderThread = ThreadHelper.JoinableTaskFactory.RunAsync(async () => Output = await ReadByteAsync(Process.StandardError.BaseStream).ConfigureAwait(false));
        }

        public async Task WaitForExitAsync()
        {
            await _stdOutputLoaderThread.JoinAsync().ConfigureAwait(false);
            await _stdErrLoaderThread.JoinAsync().ConfigureAwait(false);
            await Process.WaitForExitAsync().ConfigureAwait(false);
        }

        public string OutputString(Encoding encoding)
        {
            return encoding.GetString(Output);
        }

        public string ErrorString(Encoding encoding)
        {
            return encoding.GetString(Error);
        }

        /// <summary>
        /// This function reads the output to a string. This function can be dangerous, because it returns a string
        /// and needs to know the correct encoding. This function should be avoided!
        /// </summary>
        public static async Task<(string stdOutput, string stdError)> ReadAsync(Process process)
        {
            var stdOutputTask = process.StandardOutput.ReadToEndAsync();
            var stdError = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            var stdOutput = await stdOutputTask.ConfigureAwait(false);
            return (stdOutput, stdError);
        }

        /// <summary>
        /// This function reads the output to a byte[]. This function is used because it doesn't need to know the
        /// correct encoding.
        /// </summary>
        public static async Task<(byte[] stdOutput, byte[] stdError)> ReadBytesAsync(Process process)
        {
            var stdOutputTask = ReadByteAsync(process.StandardOutput.BaseStream);
            var stdError = await ReadByteAsync(process.StandardError.BaseStream).ConfigureAwait(false);
            var stdOutput = await stdOutputTask.ConfigureAwait(false);
            return (stdOutput, stdError);
        }

        private static async Task<byte[]> ReadByteAsync(Stream stream)
        {
            if (!stream.CanRead)
            {
                return null;
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream).ConfigureAwait(false);
                return memStream.ToArray();
            }
        }
    }
}
