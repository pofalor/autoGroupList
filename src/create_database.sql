drop table Students;
drop table schedule;
drop table attendance;
drop table leader;
drop table notifications;



CREATE TABLE Students
(
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(100) NOT NULL,
    number_in_group INT UNIQUE   NOT NULL,
    telegram_id     BIGINT UNIQUE,
    subgroup        VARCHAR(50)
);

CREATE TABLE Schedule
(
    id          SERIAL PRIMARY KEY,
    subgroup    VARCHAR(50)  NOT NULL,
    week_type   VARCHAR(10)  NOT NULL,
    day_of_week VARCHAR(10)  NOT NULL,
    subject     VARCHAR(100) NOT NULL,
    start_time  VARCHAR(5)   NOT NULL,
    end_time    VARCHAR(5)   NOT NULL
);

CREATE TABLE Attendance
(
    id         SERIAL PRIMARY KEY,
    student_id INT REFERENCES Students (id) ON DELETE CASCADE,
    subject    VARCHAR(100)             NOT NULL,
    date       DATE                     NOT NULL,
    is_present BOOLEAN                  NOT NULL,
    timestamp  TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT unique_attendance_entry UNIQUE (student_id, subject, date)
);

CREATE TABLE Leader
(
    id          SERIAL PRIMARY KEY,
    student_id  INT           REFERENCES Students (id) ON DELETE SET NULL,
    telegram_id BIGINT UNIQUE NOT NULL
);

CREATE TABLE Notifications
(
    id SERIAL PRIMARY KEY,
    student_id INT REFERENCES Students(id) ON DELETE CASCADE,
    subject VARCHAR(100),
    date DATE NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    is_sent BOOLEAN NOT NULL DEFAULT FALSE,
    sent_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

INSERT INTO Students (name, number_in_group) VALUES
('Абделфаттах Ахмед', 1),
('Абейдуллов Алмаз', 2),
('Аль-Гурайри Зайд', 3),
('Ахметгалеев Ислам', 4),
('Багаутдинов Артём', 5),
('Валиев Нияз', 6),
('Габдулхаков Разиль', 7),
('Гаврилов Николай', 8),
('Галимуллин Марсель', 9),
('Гатауллин Тимур', 10),
('Громов Даниил', 11),
('Елазаб Мохамед', 12),
('Йолдошева Умидабону', 13),
('Марцинко Александр', 14),
('Миндубаев Инсаф', 15),
('Мохамед Магед', 16),
('Муфтиев Ильяс', 17),
('Саниев Руслан', 18),
('Сафиуллин Ранис', 19),
('Тихонов Андрей', 20),
('Французов Денис', 21),
('Хамидуллин Булат', 22),
('Хузиев Ратмир', 23),
('Хузин Камиль', 24),
('Шагеева Лейла', 25),
('Шайдуллин Марсель', 26),
('Щеняев Ярослав', 27);

INSERT INTO Schedule (subgroup, week_type, day_of_week, subject, start_time, end_time) VALUES
('subgroup1', 'even', 'monday', 'ВЫЧМАТ', '08:00', '09:30'),
('subgroup1', 'even', 'wednesday', 'ТФЯМТ', '13:30', '15:00'),
('subgroup1', 'even', 'wednesday', 'ПАПС', '15:10', '16:40'),
('subgroup1', 'even', 'thursday', 'Веб', '11:20', '12:50'),
('subgroup1', 'even', 'thursday', 'ПАПС', '13:30', '15:00'),
('subgroup1', 'even', 'friday', 'Веб', '08:00', '11:10'),
('subgroup1', 'even', 'saturday', 'Java', '11:20', '15:00'),
('subgroup1', 'even', 'saturday', 'ПАПС / ТФЯМТ', '15:10', '18:20'),
('subgroup1', 'odd', 'monday', 'Transact SQL', '15:10', '18:20'),
('subgroup1', 'odd', 'wednesday', 'ВЫЧМАТ', '08:00', '11:10'),
('subgroup1', 'odd', 'thursday', 'ТФЯМТ', '15:10', '16:40'),
('subgroup1', 'odd', 'thursday', 'Java', '16:50', '18:20'),
('subgroup1', 'odd', 'friday', 'ПАПС', '15:10', '16:40'),
('subgroup1', 'odd', 'friday', 'КАЧПО', '16:50', '18:20'),
('subgroup1', 'odd', 'saturday', 'КАЧПО', '11:20', '15:00'),
('subgroup1', 'odd', 'saturday', 'Веб', '15:10', '16:40'),
('subgroup2', 'even', 'monday', 'ВЫЧМАТ', '08:00', '09:30'),
('subgroup2', 'even', 'wednesday', 'ВЫЧМАТ', '08:00', '11:10'),
('subgroup2', 'even', 'wednesday', 'ТФЯМТ', '13:30', '15:00'),
('subgroup2', 'even', 'wednesday', 'ПАПС', '15:10', '16:40'),
('subgroup2', 'even', 'thursday', 'Веб', '11:20', '12:50'),
('subgroup2', 'even', 'thursday', 'ПАПС', '13:30', '15:00'),
('subgroup2', 'even', 'friday', 'КАЧПО', '08:00', '11:10'),
('subgroup2', 'even', 'saturday', 'Transact SQL', '11:20', '15:00'),
('subgroup2', 'even', 'saturday', 'ПАПС / ТФЯМТ', '15:10', '18:20'),
('subgroup2', 'odd', 'monday', 'Java', '15:10', '18:20'),
('subgroup2', 'odd', 'thursday', 'ТФЯМТ', '15:10', '16:40'),
('subgroup2', 'odd', 'thursday', 'Java', '16:50', '18:20'),
('subgroup2', 'odd', 'friday', 'ПАПС', '15:10', '16:40'),
('subgroup2', 'odd', 'friday', 'КАЧПО', '16:50', '18:20'),
('subgroup2', 'odd', 'saturday', 'Веб', '11:20', '15:00'),
('subgroup2', 'odd', 'saturday', 'Веб практика', '15:10', '16:40');

INSERT INTO Leader (student_id, telegram_id) VALUES
(11, 825126696);

ALTER TABLE Notifications
ADD CONSTRAINT notifications_unique_key UNIQUE (student_id, subject, date, notification_type);

