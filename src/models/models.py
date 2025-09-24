import enum
from datetime import time, datetime, date

from sqlalchemy import (ForeignKey, UniqueConstraint, Boolean,
                        Time, Date, func
                        )
from sqlalchemy.orm import Mapped, mapped_column, relationship
from src.models.database import Base

class ClassType(str, enum.Enum):
    LAB = 'Лабораторная работа'
    PRACTICE = 'Практическая работа'
    LECTURE = 'Лекция'

class NotificationType(str, enum.Enum):
    START_CLASS = 'start_class'
    END_DAY_REPORT = 'end_day_report'

class WeekType(str, enum.Enum):
    EVEN = 'even'
    ODD = 'odd'

class WeekDay(str, enum.Enum):
    MONDAY = "monday"
    TUESDAY = "tuesday"
    WEDNESDAY = "wednesday"
    THURSDAY = "thursday"
    FRIDAY = "friday"
    SATURDAY = "saturday"
    SUNDAY = "sunday"

class Student(Base):
    name: Mapped[str] = mapped_column(nullable=False)
    number_in_group: Mapped[int] = mapped_column(nullable=False, unique=True)
    telegram_id: Mapped[str] = mapped_column(nullable=False, unique=True)
    subgroup: Mapped[int]

class Subject(Base):
    name: Mapped[str] = mapped_column(nullable=False)

    schedules: Mapped[list["Schedule"]] = relationship(
        "Schedule",
        back_populates="subjects"
    )

class Schedule(Base):
    subgroup: Mapped[int] = mapped_column(nullable=False)
    week_type: Mapped[WeekType] = mapped_column(nullable=False)
    day_of_week: Mapped[WeekDay] = mapped_column(nullable=False)
    start_time: Mapped[time] = mapped_column(Time)
    end_time: Mapped[time] = mapped_column(Time)
    subject_id: Mapped[int] = mapped_column(ForeignKey('subjects.id', ondelete='RESTRICT'), nullable=False)
    class_type: Mapped[ClassType] = mapped_column(nullable=False)
    subject: Mapped["Subject"] = relationship(
        "Subject",
        back_populates="schedules"
    )

class Attendance(Base):
    student_id: Mapped[int] = mapped_column(ForeignKey('students.id', ondelete='RESTRICT'), nullable=False)
    schedule_id: Mapped[int] = mapped_column(ForeignKey('schedule.id', ondelete='RESTRICT'), nullable=False)
    date: Mapped[date] = mapped_column(Date, nullable=False)
    UniqueConstraint('student_id', 'subject', 'date')

class Leader(Base):
    student_id: Mapped[int | None] = mapped_column(ForeignKey('students.id', ondelete='SET NULL'))

class Notifications(Base):
    student_id: Mapped[int | None] = mapped_column(ForeignKey('students.id', ondelete='RESTRICT'), nullable=False)
    schedule_id: Mapped[int] = mapped_column(ForeignKey('schedule.id', ondelete='RESTRICT'), nullable=False)
    date: Mapped[date] = mapped_column(Date, nullable=False)
    notification_type: Mapped[ClassType] = mapped_column(name='class_type', nullable=False)
    is_sent: Mapped[bool] = mapped_column(Boolean, nullable=False, server_default='false',)
    sent_at: Mapped[datetime] = mapped_column(nullable=False, server_default=func.now())