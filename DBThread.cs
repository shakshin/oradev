using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace oradev
{
    public enum ThreadTaskType
    {
        Query, Execute, ExplainPlan
    }

    public delegate void ProcessThreadedResult(DataTable result, long elapsed);

    public class DBThreadTask
    {
        public ThreadTaskType Type;
        public String Text;
        public ProcessThreadedResult Callback;
    }

    public class DBThread
    {
        private DataBaseConfig _config;
        private OracleConnection _connection = null;
        private OracleTransaction _tran = null;
        private Thread _thread = null;
        private AutoResetEvent _wake = new AutoResetEvent(false);
        private Boolean _ready = false;
        private Object _lockHandler = new Object();

        private ConcurrentQueue<DBThreadTask> _tasks = null;

        public void Init(DataBaseConfig config)
        {
            Stop();
            _config = config;
            
        }

        public void Start()
        {
            try
            {
                _tasks = new ConcurrentQueue<DBThreadTask>();
                _connection = Oracle.GetConnection(_config);
                _connection.Open();
                _tran = _connection.BeginTransaction();
            }
            catch (Exception e)
            {
                Console.Log("Error creating Oracle sesssion: " + e.Message);
                lock (_lockHandler)
                {
                    _ready = false;
                }
                return;
            }
            lock (_lockHandler)
            {
                _ready = true;
            }
            DBThreadTask task = null;
            _thread = new Thread(delegate()
            {
                try
                {
                    while (true)
                    {
                        if (_tasks.TryDequeue(out task))
                        {
                            lock (_lockHandler)
                            {
                                _ready = false;
                            }
                            Stopwatch sw;
                            DataTable result;
                            switch (task.Type)
                            {
                                case ThreadTaskType.Execute:
                                    sw = new Stopwatch();
                                    sw.Start();
                                    Oracle.Execute(task.Text, null, _connection, _tran);
                                    sw.Stop();
                                    lock (_lockHandler)
                                    {
                                        _ready = true;
                                    }
                                    if (task.Callback != null)
                                        App.Current.Dispatcher.Invoke((Action) delegate
                                        {
                                            task.Callback(null,
                                                sw.ElapsedMilliseconds);
                                        });

                                    break;
                                case ThreadTaskType.ExplainPlan:
                                    sw = new Stopwatch();
                                    sw.Start();
                                    result = Oracle.ExplainPlan(task.Text, null, _connection);
                                    sw.Stop();
                                    lock (_lockHandler)
                                    {
                                        _ready = true;
                                    }
                                    if (task.Callback != null)
                                        App.Current.Dispatcher.Invoke((Action) delegate
                                        {
                                            task.Callback(result,
                                                sw.ElapsedMilliseconds);
                                        });
                                    break;

                                case ThreadTaskType.Query:
                                    sw = new Stopwatch();
                                    sw.Start();
                                    result = Oracle.Query(task.Text, null, _connection, _tran);
                                    sw.Stop();
                                    lock (_lockHandler)
                                    {
                                        _ready = true;
                                    }
                                    if (task.Callback != null)
                                        App.Current.Dispatcher.Invoke((Action) delegate
                                        {
                                            task.Callback(result,
                                                sw.ElapsedMilliseconds);
                                        });
                                    break;
                            }
                        }
                        else
                        {
                            _wake.WaitOne();
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    try
                    {
                        Console.Log("Session reset complete");
                        if (task != null && task.Callback != null)
                            App.Current.Dispatcher.Invoke((Action) delegate
                            {
                                task.Callback(new DataTable(),
                                    0);
                            });
                    }
                    catch (Exception)
                    {
                    }
                }
            });
                _thread.Start();
        }

        public bool ISReady()
        {
            lock (_lockHandler)
            {
                return _ready;

            }
        }

        public void Stop()
        {
            if (_thread != null)
            {
                lock (_lockHandler)
                {
                    _ready = false;
                }
                _thread.Abort();
            }
        }

        public void Enqueue(DBThreadTask task)
        {
            if (!ISReady())
            {
               Console.Log("Session is not ready");
                return;
            }
            _tasks.Enqueue(task);
            _wake.Set();
        }



        public void Execute(String text, ProcessThreadedResult callback)
        {
            Enqueue(new DBThreadTask()
            {
                Type = ThreadTaskType.Execute,
                Text = text,
                Callback = callback
            }); 
        }

        public void Query(String text, ProcessThreadedResult callback)
        {
            Enqueue(new DBThreadTask()
            {
                Type = ThreadTaskType.Query,
                Text = text,
                Callback = callback
            });
        }

        public void ExplainPlan(String text, ProcessThreadedResult callback)
        {
            Enqueue(new DBThreadTask()
            {
                Type = ThreadTaskType.ExplainPlan,
                Text = text,
                Callback = callback
            });
        }

        public void Commit()
        {
            Enqueue(new DBThreadTask()
            {
                Type = ThreadTaskType.Query,
                Text = "commit"
            });
        }

        public void Rollback()
        {
            Enqueue(new DBThreadTask()
            {
                Type = ThreadTaskType.Query,
                Text = "rollback"
            });
        }
    }
}