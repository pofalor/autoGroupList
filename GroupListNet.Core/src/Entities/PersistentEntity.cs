using System.ComponentModel.DataAnnotations;

namespace GroupListNet.Core.src.Entities
{
    public abstract class PersistentEntity
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime ObjectCreateDate { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public DateTime ObjectEditDate { get; set; }

        /// <summary>
        /// Версия объекта
        /// </summary>
        [Timestamp]
        public byte[] Version { get; set; } = null!;

        /// <summary>
        /// Поле означающее удалена сущность или нет
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
