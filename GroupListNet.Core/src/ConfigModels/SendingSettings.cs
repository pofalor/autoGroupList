using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupListNet.Core.src.ConfigSectionModels
{
    public class SendingSettings
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string SendingSettingsSectionInConfig = "SendingSettings";

        /// <summary>
        /// До какого времени пользователи могут отмечаться (задано в UTC)
        /// </summary>
        public string DeadlineTime { get; set; } = string.Empty;
    }
}
