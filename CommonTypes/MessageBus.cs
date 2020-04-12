using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Diagnostics;
using System.IO;


namespace CommonTypes
{
    public interface IMessageBus
    {
        int EpochSecs { get; }

        void Send<T>(T message);

        IObservable<T> AsObservable<T>();
    }


    public class Ether : IMessageBus
    {
        Subject<object> MessageSubject;


        // This is the minimum granularity for strategies/indicators that do not operate directly
        // on Markets. For example, Bars are constrained to be of length n * EpochSecs, n >= 1.
        public int EpochSecs { get; private set; }


        public Ether(int epochSecs)
        {
            if (epochSecs != 1 && epochSecs % 2 != 0 && epochSecs % 3 != 0 && epochSecs % 5 != 0)
                throw new Exception("Ether -- error, epoch must be defined as a multiple of 2s, 3s or 5s or equal 1!");

            EpochSecs = epochSecs;
            MessageSubject = new Subject<object>();
        }


        public void Send<T>(T message)
        {
            // Since everything's on the same thread, this needs to be here (rather than after the Rx call)
            // in order for things to show up in the right order. Otherwise, this will be printed as the
            // stack unwinds and everything will appear to be happening backwards.
            //File.AppendAllText(@"C:\temp\Zeus_MessageBusOutput.txt", string.Format("{0} ({1}): {2}\r\n",
            //                   message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), message.GetType().ToString(), message.ToString()));

            System.Reactive.Concurrency.Scheduler.Immediate.Schedule(() => MessageSubject.OnNext(message));
        }


        public IObservable<T> AsObservable<T>()
        {
            return MessageSubject.OfType<T>();
        }
    }
}
