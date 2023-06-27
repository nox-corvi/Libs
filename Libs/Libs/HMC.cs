using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nox.HMC
{
    public class ChannelDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";

        public int Index { get; set; } = 0;
        public int ParentDevice { get; set; }

        public int Type { get; set; } = 0;
        public string Direction { get; set; } = "";

        public string GroupPartner { get; set; } = "";

        public bool AESAvailable { get; set; } = false;
        public string TransmissionMode { get; set; } = "";

        public bool Visible { get; set; } = false;
        public bool ReadyConfig { get; set; } = false;
        public bool Operate { get; set; } = false;
    }

    public class RoomDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public List<int> Channels { get; set; } = new();
    }

    public class DeviceDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string Interface { get; set; } = "";

        public string DeviceType { get; set; } = "";

        public bool ReadyConfig = false;

        public List<ChannelDescriptor> Channels { get; set; } = new();
    }

    public class NotificationDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public string Timestamp { get; set; } = "";
    }

    public class ProtocolDescriptor
    {
        public string DateTime { get; set; } = "";
        public string Names { get; set; } = "";

        public string Values { get; set; } = "";

        public string Timestamp { get; set; } = "";
    }

    public class RSSIDescriptor
    {
        public string Device { get; set; } = "";
        public int RX { get; set; } = 0;
        public int TX { get; set; } = 0;
    }

    public class DatapointDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";

        public string Type { get; set; } = "";

        public string Value { get; set; } = "";
        public int ValueType { get; set; } = 0;
        public string ValueUnit { get; set; } = "";

        public string Timestamp { get; set; } = "";
        public int Operations { get; set; } = 0;
    }

    public class ChannelStateDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";

        public int Index { get; set; } = 0;
        public string Visible { get; set; } = "";
        public string Operate { get; set; } = "";

        public List<DatapointDescriptor> Datapoints = new();
    }

    public class DeviceStateDescriptor
    {

        public int DeviceId { get; set; } = -1;
        public string DeviceName { get; set; } = "";
        public string DeviceType { get; set; } = "";

        public bool ReadyConfig { get; set; } = false;

        public List<ChannelStateDescriptor> Channels = new();
    }

    public class SysVarDescriptor
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";

        public int Variable { get; set; } = 0;
        public string Value { get; set; } = "";
        public List<string> ValueList { get; set; } = new();

        public string Min { get; set; } = "";
        public string Max { get; set; } = "";
        public string Unit { get; set; } = "";

        public int Type { get; set; } = 0;
        public int SubType { get; set; } = 0;

        public bool Logged { get; set; } = false;
        public bool Visible { get; set; } = false;

        public string Timestamp { get; set; } = "";

        public string ValueName0 { get; set; } = "";
        public string ValueName1 { get; set; } = "";
    }

    public class HMCore0
    {
        private const string HMC_BASE_URL = "http://<IP>/config/xmlapi/*";

        private Log4 _Log = Log4.Create();

        private string _IP = "";
        private string _Version = "";

        #region Properties
        public string IP { get => _IP; }
        public string Version
        {
            get
            {
                if (_Version == "")
                    _Version = GetVersion();

                return _Version;
            }
        }
        #endregion

        public HMCore0(string IP) =>
            _IP = IP;

        #region Query-Tools 
        private string BaseUri(string Query)
        {
            ArgumentNullException.ThrowIfNull(Query);
            return HMC_BASE_URL.Replace("<IP>", _IP).Replace("*", Query);
        }

        private string HttpRequest(string Uri)
        {
            var r = (new HttpClient()).GetStringAsync(Uri);
            r.Wait();

            return r.Result;
        }

        private string GetVersion() =>
            XDocument.Parse(HttpRequest(BaseUri("version.cgi")))?.Root?.Value?.ToString() ?? "";

        public List<RoomDescriptor> GetRoomList()
        {
            try
            {
                string content = HttpRequest(BaseUri("roomlist.cgi"));

                if (content != null)
                    return XDocument.Parse(content).Root.Elements("room")
                           .Select(f => new RoomDescriptor()
                           {
                               Id = int.Parse(f.Attribute("ise_id").Value),
                               Name = f.Attribute("name").Value ?? "",
                               Channels = f.Elements("channel").Select(h => int.Parse(h.Attribute("ise_id").Value)).ToList(),
                           }).ToList();

                else
                    return new List<RoomDescriptor>();
            }
            catch (OperationCanceledException)
            {
                return new List<RoomDescriptor>();
            }
        }
        public List<DeviceDescriptor> GetDeviceList()
        {
            try
            {
                string content = HttpRequest(BaseUri("devicelist.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("device")
                        .Select(f => new DeviceDescriptor()
                        {
                            Id = int.Parse(f.Attribute("ise_id").Value),
                            Name = f.Attribute("name").Value ?? "",
                            Address = f.Attribute("address").Value ?? "",
                            Interface = f.Attribute("interface").Value ?? "",

                            DeviceType = f.Attribute("device_type").Value ?? "",

                            ReadyConfig = bool.Parse(f.Attribute("ready_config").Value),

                            Channels = f.Elements("channel").Select(h => new ChannelDescriptor()
                            {
                                Id = int.Parse(h.Attribute("ise_id").Value),
                                Name = h.Attribute("name").Value ?? "",
                                Address = h.Attribute("address").Value ?? "",

                                Index = int.Parse(h.Attribute("index").Value),
                                ParentDevice = int.Parse(h.Attribute("parent_device").Value),

                                Type = int.Parse(h.Attribute("type").Value ?? ""),
                                Direction = h.Attribute("direction").Value ?? "",

                                GroupPartner = h.Attribute("group_partner").Value ?? "",

                                AESAvailable = bool.Parse(h.Attribute("aes_available").Value),
                                TransmissionMode = h.Attribute("transmission_mode").Value ?? "",

                                Visible = bool.Parse(h.Attribute("visible").Value),
                                ReadyConfig = bool.Parse(h.Attribute("ready_config").Value),
                                Operate = bool.Parse(h.Attribute("operate").Value),
                            }).ToList(),
                        }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<RSSIDescriptor> GetRSSI()
        {
            try
            {
                var content = HttpRequest(BaseUri("rssilist.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("rssi")
                    .Select(f => new RSSIDescriptor()
                    {
                        Device = f.Attribute("device").Value ?? "",
                        RX = int.Parse(f.Attribute("rx").Value),
                        TX = int.Parse(f.Attribute("tx").Value),
                    }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<NotificationDescriptor> GetNotifications()
        {
            try
            {
                var content = HttpRequest(BaseUri("systemNotification.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("notification")
                    .Select(f => new NotificationDescriptor()
                    {
                        Id = int.Parse(f.Attribute("ise_id").Value),
                        Name = f.Attribute("name").Value ?? "",

                        Type = f.Attribute("type").Value ?? "",
                        Timestamp = f.Attribute("timestamp").Value ?? "",
                    }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<ProtocolDescriptor> GetProtocol()
        {
            try
            {
                var content = HttpRequest(BaseUri("protocol.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("row")
                    .Select(f => new ProtocolDescriptor()
                    {
                        DateTime = f.Attribute("datetime").Value ?? "",
                        Names = f.Attribute("names").Value ?? "",
                        Values = f.Attribute("values").Value ?? ""
                    }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }

        }
        public bool ProtocolClear()
        {
            try
            {
                var content = HttpRequest(BaseUri("protocol.cgi?clear=1"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("cleared_protocol").FirstOrDefault() != null;
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<DeviceStateDescriptor> GetStates()
        {
            try
            {
                var content = HttpRequest(BaseUri("statelist.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("device")
                    .Select(x => new DeviceStateDescriptor()
                    {
                        DeviceId = int.Parse(x.Attribute("ise_id").Value),
                        DeviceName = x.Attribute("name").Value ?? "",

                        Channels = x.Elements("channel")
                            .Select(y => new ChannelStateDescriptor()
                            {
                                Id = int.Parse(y.Attribute("ise_id").Value),
                                Name = y.Attribute("name").Value ?? "",

                                Index = int.Parse(y.Attribute("index").Value),
                                Visible = y.Attribute("visible").Value ?? "",
                                Operate = y.Attribute("operate").Value ?? "",

                                Datapoints = y.Elements("datapoint")
                                    .Select(z => new DatapointDescriptor()
                                    {
                                        Id = int.Parse(z.Attribute("ise_id").Value),
                                        Name = z.Attribute("name").Value ?? "",
                                        Type = z.Attribute("type").Value ?? "",
                                        Value = z.Attribute("value").Value ?? "",
                                        ValueType = int.Parse(z.Attribute("valuetype").Value),
                                        ValueUnit = z.Attribute("valueunit").Value ?? "",
                                        Timestamp = z.Attribute("timestamp").Value ?? "",
                                        Operations = int.Parse(z.Attribute("operations").Value),
                                    }).ToList(),
                            }).ToList()
                    }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<DeviceStateDescriptor> GetDeviceState(int device_id)
        {
            try
            {
                var content = HttpRequest(BaseUri($"state.cgi?device_id={device_id}"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("device")
                .Select(x => new DeviceStateDescriptor()
                {
                    DeviceId = int.Parse(x.Attribute("ise_id").Value),
                    DeviceName = x.Attribute("name").Value ?? "",

                    Channels = x.Elements("channel")
                        .Select(y => new ChannelStateDescriptor()
                        {
                            Id = int.Parse(y.Attribute("ise_id").Value),
                            Name = y.Attribute("name").Value ?? "",

                            Index = int.Parse(y.Attribute("index")?.Value ?? "0"),
                            Visible = y.Attribute("visible")?.Value ?? "",
                            Operate = y.Attribute("operate")?.Value ?? "",

                            Datapoints = y.Elements("datapoint")
                                .Select(z => new DatapointDescriptor()
                                {
                                    Id = int.Parse(z.Attribute("ise_id").Value),
                                    Name = z.Attribute("name").Value ?? "",
                                    Type = z.Attribute("type")?.Value ?? "",
                                    Value = z.Attribute("value")?.Value ?? "",
                                    ValueType = int.Parse(z.Attribute("valuetype")?.Value ?? "0"),
                                    ValueUnit = z.Attribute("valueunit")?.Value ?? "",
                                    Timestamp = z.Attribute("timestamp")?.Value ?? "",
                                    Operations = int.Parse(z.Attribute("operations")?.Value ?? "0"),
                                }).ToList(),
                        }).ToList()
                }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public DatapointDescriptor GetDatapoint(int Id)
        {
            try
            {
                var content = HttpRequest(BaseUri($"state.cgi?datapoint_id={Id}"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("datapoint")
                    .Select(x => new DatapointDescriptor()
                    {
                        Id = int.Parse(x.Attribute("ise_id").Value),
                        Value = x.Attribute("value").Value ?? ""
                    }).First();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<DatapointDescriptor> GetChannelDatapoint(int Id)
        {
            try
            {
                var content = HttpRequest(BaseUri($"state.cgi?datapoint_id={Id}"));
                if (content != null)
                {
                    var f = XDocument.Parse(content);
                    var Result = new List<DatapointDescriptor>();
                    foreach (var c in f.Root.Elements("device"))
                        foreach (var d in c.Elements("channel"))
                            if (((string?)d?.Attribute("ise_id") ?? "") == Id.ToString())
                                foreach (var item in d.Elements("datapoint"))
                                {
                                    Result.Add(new DatapointDescriptor()
                                    {
                                        Id = int.Parse(item.Attribute("ise_id").Value),
                                        Name = (string)item.Attribute("name") ?? "",
                                        Type = (string)item.Attribute("type") ?? "",
                                        Value = item.Attribute("value").Value ?? "",
                                        Operations = int.Parse((string)item.Attribute("operations") ?? "0"),
                                        Timestamp = (string)item.Attribute("timestamp") ?? "",
                                        ValueType = int.Parse((string)item.Attribute("valuetype") ?? "0"),
                                        ValueUnit = (string)item.Attribute("valueunit") ?? "",

                                    });
                                }

                    return Result.ToList();
                }
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public List<SysVarDescriptor> GetSysVariables()
        {
            try
            {
                var content = HttpRequest(BaseUri("sysvarlist.cgi"));
                if (content != null)
                    return XDocument.Parse(content).Root.Elements("systemVariable")
                .Select(f => new SysVarDescriptor()
                {
                    Id = int.Parse(f.Attribute("ise_id").Value),

                    Name = f.Attribute("name").Value,

                    Variable = int.Parse(f.Attribute("variable").Value),
                    Value = f.Attribute("value").Value,
                    ValueList = f.Attribute("value_list").Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),

                    Min = f.Attribute("min").Value,
                    Max = f.Attribute("max").Value,
                    Unit = f.Attribute("unit").Value,

                    Type = int.Parse(f.Attribute("type").Value),
                    SubType = int.Parse(f.Attribute("subtype").Value),

                    Logged = bool.Parse(f.Attribute("logged").Value),
                    Visible = bool.Parse(f.Attribute("visible").Value),

                    Timestamp = f.Attribute("timestamp").Value,

                    ValueName0 = f.Attribute("value_name_0").Value,
                    ValueName1 = f.Attribute("value_name_1").Value,
                }).ToList();
                else
                    return new();
            }
            catch (OperationCanceledException)
            {
                return new();
            }
        }

        public bool ChangeStateRaw(int Id, string NewValue)
        {
            try
            {
                var content = HttpRequest(BaseUri($"state.cgi?datapoint_id={Id}"));
                if (content != null)
                {
                    var Response = XDocument.Parse(HttpRequest(BaseUri($"statechange.cgi?ise_id={Id}&new_value={NewValue}"))).Root;

                    if (Response.Elements("not_found").FirstOrDefault() != null)
                        return false;
                    else
                    {
                        //changed id = "3568" new_value = "1" />
                        var Changed = Response.Elements("changed").FirstOrDefault();

                        if (Changed != null)
                        {
                            var ChangedId = int.Parse(Changed.Attribute("ise_id").Value);
                            var ChangedNewValue = Changed.Attribute("new_value").Value;

                            if (ChangedId != Id)
                                return false;

                            if (ChangedNewValue != NewValue)
                                return false;

                            // well done
                            return true;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;   
            }
                catch (OperationCanceledException)
            {
                return new();
            }
        }
        #endregion
    }
}
