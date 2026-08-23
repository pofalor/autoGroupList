namespace GroupListNet.Core.src.Constants
{
    public static class DateFormatConstants
    {
        /// <summary>
        /// формат даты dd.MM.yyyy HH:mm:ss.FFFFFFF
        /// </summary>
        public const string FullDateTime = "dd.MM.yyyy HH:mm:ss.FFFFFFF";

        /// <summary>
        /// формат даты dd.MM.yyyy HH:mm:ss
        /// </summary>
        public const string FullDateTimeShort = "dd.MM.yyyy HH:mm:ss";

        /// <summary>
        /// ISO формат даты UTC yyyy-MM-ddTHH:mm:ss
        /// </summary>
        public const string IsoString = "yyyy-MM-ddTHH:mm:ss";

        /// <summary>
        /// Вывод даты dd.MM.yyyy без указания времени 
        /// </summary>
        public const string DatewithoutTimeZone = "dd.MM.yyyy";

        /// <summary>
        /// Вывод даты yyyy-MM-dd без указания времени 
        /// </summary>
        public const string FrontInputFormat = "yyyy-MM-dd";

        /// <summary>
        /// полный формат даты, валидрый для API "yyyy-MM-ddTHH-mm-ss-FFFFFFF
        /// </summary>
        public const string ApiFullDateTimeString = "yyyy-MM-ddTHH-mm-ss-FFFFFFF";

        /// <summary>
        /// Вывод даты день.месяц.год время dd.MM.yy HH:mm
        /// </summary>
        public const string CalendarFormat = "dd.MM.yy HH:mm";

        /// <summary>
        /// dd.MM.yy HH:mm:ss
        /// </summary>
        public const string DateTimeWithSeconds = "dd.MM.yy HH:mm:ss";

        /// <summary>
        /// dd.MM.yyyy_HH_mm
        /// </summary>
        public const string FileExportFormat = "dd.MM.yyyy_HH_mm";

        /// <summary>
        /// Формат даты для записи даты в БД
        /// </summary>
        public const string DataBaseTime = "yyyy-MM-dd HH:mm:ss.FFFFFF";

        /// <summary>
        /// dd.MM.yy
        /// </summary>
        public const string ShortYear = "dd.MM.yy";

        /// <summary>
        /// формат даты utc yyyy-MM-ddTHH:mm:ssZ
        /// </summary>
        public const string FullDateTimeUTC = "yyyy-MM-ddTHH:mm:ssZ";
    }
}
