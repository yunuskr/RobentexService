using System.Collections.Generic;
using RobentexService.Models;

namespace RobentexService.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public IReadOnlyList<ServiceRequest> YeniTalep { get; set; } = new List<ServiceRequest>();
        public IReadOnlyList<ServiceRequest> TeklifIletildi { get; set; } = new List<ServiceRequest>();
        public IReadOnlyList<ServiceRequest> ServisAsamasi { get; set; } = new List<ServiceRequest>();
        public IReadOnlyList<ServiceRequest> TeklifReddedildi { get; set; } = new List<ServiceRequest>();
        public IReadOnlyList<ServiceRequest> Tamamlandi { get; set; } = new List<ServiceRequest>();
        public IReadOnlyList<ServiceRequest> FaturaEdildi { get; set; } = new List<ServiceRequest>();
    }
}
