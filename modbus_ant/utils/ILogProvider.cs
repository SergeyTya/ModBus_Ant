﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace modbus_ant.utils
{
    public interface ILogProvider
    {
        public IObservable<string> GetObservable { get; }

        public void Post(string ? message);

    }
}
