namespace GroupListNet.Core.src.DataResult
{
    using System.Collections.Generic;

    public interface IDataResult
    {
        /// <summary>
        /// Ошибки
        /// </summary>
        IList<IDataError> Errors { get; }

        /// <summary>
        /// Флаг успешности выполнения
        /// </summary>
        bool Success { get; }
    }
}