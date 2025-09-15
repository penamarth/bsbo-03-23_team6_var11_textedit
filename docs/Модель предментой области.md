# Domain Model
@startuml

entity "Пользователь" as User

entity Document {
    name : string
    content : string
    format : string
}

entity Editor

entity FileManager

entity Printer

entity SyntaxHighlighter

entity HighlightToken {
    type : string
    value : string
    color : string
}

User "1" -down- "1" Editor : Работает через
Editor "1" -down- "1" Document : Редактирует
Editor "1" -down- "1" FileManager : Использует
Editor "1" -down- "1" Printer : Передает на печать
Editor "1" -down- "1" SyntaxHighlighter : Вызывает
SyntaxHighlighter "1" -down- "*" HighlightToken : Создаёт

@enduml
