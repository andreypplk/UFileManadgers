using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ufm
{
    public interface IRefreshablePanel
    {
        void RefreshNavigation();
        string PanelId { get; }
    }
}
