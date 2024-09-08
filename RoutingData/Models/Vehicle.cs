﻿using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Vehicle
    {
        public static readonly String[] VEHICLE_STATUSES = { "Active", "Inactive" };
        [Key]
        public string LicensePlate { get; set; }
        public string Status { get; set; }
    }
}
