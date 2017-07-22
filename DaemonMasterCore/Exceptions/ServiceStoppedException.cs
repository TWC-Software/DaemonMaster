/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceStoppedException
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace DaemonMasterCore.Exceptions
{
    [Serializable]
    public class ServiceStoppedException : Exception
    {
        public ServiceStoppedException()
        { }

        public ServiceStoppedException(string message) : base(message)
        { }

        public ServiceStoppedException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
