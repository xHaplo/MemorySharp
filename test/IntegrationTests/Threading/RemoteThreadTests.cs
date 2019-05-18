﻿/*
 * MemorySharp Library
 * http://www.binarysharp.com/
 *
 * Copyright (C) 2012-2016 Jämes Ménétrey (a.k.a. ZenLulz).
 * This library is released under the MIT License.
 * See the file LICENSE for more information.
*/
using System;
using System.Linq;
using System.Threading;
using Binarysharp.MemoryManagement.Native;
using Binarysharp.MemoryManagement.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemorySharpTests.Threading
{
    [TestClass]
    public class RemoteThreadTests
    {
        /// <summary>
        /// Suspends and resumes the main thread.
        /// </summary>
        [TestMethod]
        public void SuspendResumeMainThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;
            var thread = sharp.Threads.MainThread;

            // Act - Assert
            thread.Suspend();
            Assert.IsTrue(thread.IsSuspended, "The thread is not suspended.");
            thread.Resume();
            Assert.IsFalse(thread.IsSuspended, "The thread is still suspended.");
            using (thread.Suspend())
            {
                Assert.IsTrue(thread.IsSuspended, "The thread is not suspended (2).");
            }
            Assert.IsFalse(thread.IsSuspended, "The thread is still suspended (2).");
        }

        /// <summary>
        /// Kill a main thread.
        /// </summary>
        [TestMethod]
        public void TerminateMainThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;
            var thread = sharp.Threads.MainThread;

            // Act - Assert
            Assert.IsTrue(thread.IsAlive);
            thread.Terminate();
            Thread.Sleep(1000);
            Assert.IsTrue(thread.IsTerminated);

            Resources.Restart();
        }

        /// <summary>
        /// Gets the context of the main thread.
        /// </summary>
        [TestMethod]
        public void GetContextMainThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;

            // Act
#if x86
            var context = new ThreadContext32(ThreadContextFlags.All);
            sharp.Threads.MainThread.GetContext(ref context);
#elif x64
            throw new NotImplementedException();
#endif

            // Assert
            Assert.AreNotEqual(0, context.Eip);
        }

        /// <summary>
        /// Gets all segment addresses of the main thread.
        /// </summary>
        [TestMethod]
        public void GetRealSegmentAddress_GetAllSegmentsMainThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;

            // Act
#if x86
            var context = new ThreadContext32(ThreadContextFlags.All);
            sharp.Threads.MainThread.GetContext(ref context);
#elif x64
            var context = new ThreadContext32(ThreadContextFlags.All);
            sharp.Threads.MainThread.GetContext(ref context);
#endif

            var thread = sharp.Threads.MainThread;
            var fs = ((WindowsRemoteThread)thread).GetRealSegmentAddress(SegmentRegisters.Fs, ref context);

            // Assert.
            Assert.AreNotEqual(IntPtr.Zero, fs, "The FS segment is null.");
        }

        /// <summary>
        /// Copies the TlsSlots from a thread to the main one by erasing them (YEAH, it's a bit evil :D).
        /// </summary>
        [TestMethod]
        public void TebTlsSlots_CopyTlsFromAnotherThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;
            var t1 = sharp.Threads.MainThread;
            var t2 = sharp.Threads.RemoteThreads.Last();
            var values = new[] { new IntPtr(0x1123344), new IntPtr(0x55667788) };

            // Act
            // Write identifiable data
            t2.Teb.TlsSlots = values;
            // Erase main tls :O
            t1.Teb.TlsSlots = t2.Teb.TlsSlots;

            // Assert
            Assert.AreEqual(values[0], t1.Teb.TlsSlots[0], "Couldn't read/write TLS.");
            Assert.AreEqual(values[1], t1.Teb.TlsSlots[1], "Couldn't read/write TLS (2).");

            Resources.Restart();
        }

        /// <summary>
        /// Changes the EIP register.
        /// </summary>
        [TestMethod]
        public void SuspendResumeSetContextEip()
        {
            // Arrange
            var sharp = Resources.MemorySharp;
            var thread = sharp.Threads.MainThread;
            const uint newEip = 0x666;

            // Act
            using (thread.Suspend())
            {
                // Get the original value
#if x86
                var context = new ThreadContext32(ThreadContextFlags.All);
                sharp.Threads.MainThread.GetContext(ref context);
                var originalEip = context.Eip;

                //Set the value
                context.Eip = newEip;
#elif x64
                var context = new ThreadContext32(ThreadContextFlags.All);
                sharp.Threads.MainThread.GetContext(ref context);
#endif
                thread.SetContext(ref context);

                // Get the context again to validate the change
                thread.GetContext(ref context);

                // Assert
#if x86
                Assert.AreEqual(newEip, context.Eip, "The values are not equal.");
#elif x64
                throw new NotImplementedException();
#endif

                // Set the original value back
#if x86
                context.Eip = originalEip;
#elif x64
#endif
                thread.SetContext(ref context);
            }
        }

        /// <summary>
        /// Waits on the main thread during 3 seconds or until it terminates..
        /// </summary>
        [TestMethod]
        public void Join3SecMainThread()
        {
            // Arrange
            var sharp = Resources.MemorySharp;

            // Act
            var ret = sharp.Threads.MainThread.Join(TimeSpan.FromSeconds(3));

            // Assert
            Assert.AreEqual(WaitValues.Timeout, ret);
        }
    }
}
