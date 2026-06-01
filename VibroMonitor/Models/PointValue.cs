using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace VibroMonitor.Models
{
    public class PointValue
    {
        public DateTime Time { get; set; }

        public double Value { get; set; }
    }

    public class PointHistory
    {
        public int Id { get; set; }
        public int EquipmentPointId { get; set; }
        public DateTime Time { get; set; }
        public double Value { get; set; }
    }
}
