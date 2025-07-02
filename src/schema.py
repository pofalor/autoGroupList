"""
Data structures, used in project.

You may do changes in tables here, then execute
`alembic revision --message="Your text" --autogenerate`
and alembic would generate new migration for you
in staff/alembic/versions folder.
"""

import enum

from sqlalchemy import (
    Column, Enum, Integer, MetaData, SmallInteger, String, Table, BigInteger, ForeignKey, UniqueConstraint, Boolean,
    DateTime
)

# Default naming convention for all indexes and constraints
# See why this is important and how it would save your time:
# https://alembic.sqlalchemy.org/en/latest/naming.html
convention = {
    'all_column_names': lambda constraint, table: '_'.join([
        column.name for column in constraint.columns.values()
    ]),
    'ix': 'ix__%(table_name)s__%(all_column_names)s',
    'uq': 'uq__%(table_name)s__%(all_column_names)s',
    'ck': 'ck__%(table_name)s__%(constraint_name)s',
    'fk': (
        'fk__%(table_name)s__%(all_column_names)s__'
        '%(referred_table_name)s'
    ),
    'pk': 'pk__%(table_name)s'
}


class ClassType(enum.Enum):
    lab = 'Лабораторная работа'
    practice = 'Практическая работа'
    lecture = 'Лекция'

class NotificationType(enum.Enum):
    start_class = 'start_class'
    end_day_report = 'end_day_report'

# Registry for all tables
metadata = MetaData(naming_convention=convention)

# Таблица students
students = Table(
    'students',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('name', String(collation='ru-RU-x-icu'), nullable=False),
    Column('number_in_group', SmallInteger, nullable=False, unique=True),
    Column('telegram_id', BigInteger, nullable=False, unique=True),
    Column('subgroup', SmallInteger)
)

# Таблица subjects
subjects = Table(
    'subjects',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('name', String(collation='ru-RU-x-icu'), nullable=False)
)

# Таблица Schedule
schedule = Table(
    'schedule',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('subgroup', SmallInteger, nullable=False),
    Column('week_type', String(4), nullable=False),
    Column('day_of_week', String(10), nullable=False),
    Column('start_time', String(5), nullable=False),
    Column('end_time', String(5), nullable=False),
    Column('subject_id', Integer, ForeignKey('subjects.id', ondelete='RESTRICT'), nullable=False),
    Column('class_type', Enum(ClassType, name='class_type'), nullable=False),
)

# Таблица Attendance
attendance = Table(
    'attendance',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('student_id', Integer, ForeignKey('students.id', ondelete='RESTRICT'), nullable=False),
    Column('schedule_id', Integer, ForeignKey('schedule.id', ondelete='RESTRICT'), nullable=False),
    Column('date', DateTime, nullable=False),
    Column('is_present', Boolean, nullable=False, default=False),
    Column('server_date', DateTime(timezone=True), nullable=False),
    UniqueConstraint('student_id', 'subject', 'date')
)

# Таблица Leader
leader = Table(
    'leader',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('student_id', Integer, ForeignKey('students.id', ondelete='SET NULL'))
)

# Таблица Notifications
notifications = Table(
    'notifications',
    metadata,
    Column('id', Integer, primary_key=True, autoincrement=True),
    Column('student_id', Integer, ForeignKey('students.id', ondelete='RESTRICT'), nullable=False),
    Column('schedule_id', Integer, ForeignKey('schedule.id', ondelete='RESTRICT'), nullable=False),
    Column('date', DateTime, nullable=False),
    Column('notification_type', Enum(ClassType, name='class_type'), nullable=False),
    Column('is_sent', Boolean, server_default='false', nullable=False),
    Column('sent_at', DateTime(timezone=True), server_default='now()')
)

