# list of students
students = [
    'Абделфаттах Ахмед',
    'Абейдуллов Алмаз',
    'Аль-Гурайри Зайд',
    'Ахметгалеев Ислам',
    'Багаутдинов Артём',
    'Валиев Нияз',
    'Габдулхаков Разиль',
    'Гаврилов Николай',
    'Галимуллин Марсель',
    'Гатауллин Тимур',
    'Громов Даниил',
    'Елазаб Мохамед',
    'Йолдошева Умидабону',
    'Марцинко Александр',
    'Миндубаев Инсаф',
    'Мохамед Магед',
    'Муфтиев Ильяс',
    'Саниев Руслан',
    'Сафиуллин Ранис',
    'Тихонов Андрей',
    'Французов Денис',
    'Хамидуллин Булат',
    'Хузиев Ратмир',
    'Хузин Камиль',
    'Шагеева Лейла',
    'Шайдуллин Марсель',
    'Щеняев Ярослав'
]

# schedule for 1 subgroup
schedule_1 = {
    'even': {
        'monday': [
            {
                'start': '8:00',
                'end': '9:30',
                'subject': 'ВЫЧМАТ'
            }
        ],
        'tuesday': [],
        'wednesday': [
            {
                'start': '13:30',
                'end': '15:00',
                'subject': 'ТФЯМТ'
            },
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ПАПС'
            }
        ],
        'thursday': [
            {
                'start': '11:20',
                'end': '12:50',
                'subject': 'Веб'
            },
            {
                'start': '13:30',
                'end': '15:00',
                'subject': 'ПАПС'
            }
        ],
        'friday': [
            {
                'start': '8:00',
                'end': '11:10',
                'subject': 'Веб'
            }
        ],
        'saturday': [
            {
                'start': '11:20',
                'end': '15:00',
                'subject': 'Java'
            },
            {
                'start': '15:10',
                'end': '18:20',
                'subject': 'ПАПС / ТФЯМТ'
            }
        ],
        'sunday': []
    },
    'odd': {
        'monday': [
            {
                'start': '15:10',
                'end': '18:20',
                'subject': 'Transact SQL'
            }
        ],
        'tuesday': [],
        'wednesday': [
            {
                'start': '8:00',
                'end': '11:10',
                'subject': 'ВЫЧМАТ'
            }
        ],
        'thursday': [
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ТФЯМТ'
            },
            {
                'start': '16:50',
                'end': '18:20',
                'subject': 'Java'
            }
        ],
        'friday': [
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ПАПС'
            },
            {
                'start': '16:50',
                'end': '18:20',
                'subject': 'КАЧПО'
            }
        ],
        'saturday': [
            {
                'start': '11:20',
                'end': '15:00',
                'subject': 'КАЧПО'
            },
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'Веб'
            }
        ],
        'sunday': []
    }
}

# schedule for 2 subgroup
schedule_2 = {
    'even': {
        'monday': [
            {
                'start': '8:00',
                'end': '9:30',
                'subject': 'ВЫЧМАТ'
            }
        ],
        'tuesday': [],
        'wednesday': [
            {
                'start': '8:00',
                'end': '11:10',
                'subject': 'ВЫЧМАТ'
            },
            {
                'start': '13:30',
                'end': '15:00',
                'subject': 'ТФЯМТ'
            },
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ПАПС'
            }
        ],
        'thursday': [
            {
                'start': '11:20',
                'end': '12:50',
                'subject': 'Веб'
            },
            {
                'start': '13:30',
                'end': '15:00',
                'subject': 'ПАПС'
            }
        ],
        'friday': [
            {
                'start': '8:00',
                'end': '11:10',
                'subject': 'КАЧПО'
            }
        ],
        'saturday': [
            {
                'start': '11:20',
                'end': '15:00',
                'subject': 'Transact SQL'
            },
            {
                'start': '15:10',
                'end': '18:20',
                'subject': 'ПАПС / ТФЯМТ'
            }
        ],
        'sunday': []
    },
    'odd': {
        'monday': [
            {
                'start': '15:10',
                'end': '18:20',
                'subject': 'Java'
            }
        ],
        'tuesday': [],
        'wednesday': [],
        'thursday': [
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ТФЯМТ'
            },
            {
                'start': '16:50',
                'end': '18:20',
                'subject': 'Java'
            }
        ],
        'friday': [
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'ПАПС'
            },
            {
                'start': '16:50',
                'end': '18:20',
                'subject': 'КАЧПО'
            }
        ],
        'saturday': [
            {
                'start': '11:20',
                'end': '15:00',
                'subject': 'Веб'
            },
            {
                'start': '15:10',
                'end': '16:40',
                'subject': 'Веб'
            }
        ],
        'sunday': []
    }
}
