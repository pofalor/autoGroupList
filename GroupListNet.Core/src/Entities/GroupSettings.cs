namespace GroupListNet.Core.src.Entities
{
    /// <summary>
    /// Настройки группы, которыми управляет староста. В таблице хранится одна актуальная запись
    /// </summary>
    public class GroupSettings : PersistentEntity
    {
        /// <summary>
        /// Делится ли группа на подгруппы. Если нет, команды работы с подгруппами недоступны
        /// </summary>
        public bool HasSubgroups { get; set; }
    }
}
