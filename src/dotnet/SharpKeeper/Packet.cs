﻿namespace SharpKeeper
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Org.Apache.Jute;
    using Org.Apache.Zookeeper.Proto;

    public class Packet
    {
        private static readonly Logger LOG = Logger.getLogger(typeof(Packet));

        internal RequestHeader header;
        internal String serverPath;
        internal ReplyHeader replyHeader;
        internal IRecord response;
        internal bool finished;
        internal ZooKeeper.WatchRegistration watchRegistration;
        internal readonly byte[] data;

        /** Client's view of the path (may differ due to chroot) **/
        internal String clientPath;
        /** Servers's view of the path (may differ due to chroot) **/
        readonly IRecord request;

        internal Packet(RequestHeader header, ReplyHeader replyHeader, IRecord record, IRecord response, byte[] data, ZooKeeper.WatchRegistration watchRegistration)
        {
            this.header = header;
            this.replyHeader = replyHeader;
            request = record;
            this.response = response;
            if (data != null)
            {
                this.data = data;
            } 
            else
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    using (ZooKeeperBinaryWriter writer = new ZooKeeperBinaryWriter(ms))
                    {
                        BinaryOutputArchive boa = BinaryOutputArchive.getArchive(writer);
                        boa.WriteInt(-1, "len"); // We'll fill this in later
                        header.Serialize(boa, "header");
                        if (record != null)
                        {
                            record.Serialize(boa, "request");
                        }
                        ms.Position = 0;
                        writer.Write(ms.ToArray().Length - 4);
                        this.data = ms.ToArray();
                        if (LOG.IsDebugEnabled()) LOG.Debug(string.Format("Preparing message: {0}", BitConverter.ToString(this.data)));
                    }
                }
                catch (IOException e)
                {
                    LOG.Warn("Ignoring unexpected exception", e);
                }
            }
            this.watchRegistration = watchRegistration;
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        internal EventWaitHandle WaitHandle
        { 
            get; private set;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("clientPath:" + clientPath);
            sb.Append(" serverPath:" + serverPath);
            sb.Append(" finished:" + finished);

            sb.Append(" header:: " + header);
            sb.Append(" replyHeader:: " + replyHeader);
            sb.Append(" request:: " + request);
            sb.Append(" response:: " + response);

            // jute toString is horrible, remove unnecessary newlines
            return sb.ToString().Replace(@"\r*\n+", " ");
        }
    }
}