using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Interfaces
{
    public interface IConfigurationService
    {
        T GetValue<T>(string key, T defaultValue = default);
        void SetValue<T>(string key, T value);
        FileSearchConfig LoadFileSearchConfig();
        void SaveFileSearchConfig(FileSearchConfig config);
    }
}
