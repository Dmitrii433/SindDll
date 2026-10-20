# SindDll 🛠️

[![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.7.2+-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![VB.NET](https://img.shields.io/badge/Language-VB.NET-512BD4?style=flat-square&logo=visualbasic)](https://docs.microsoft.com/en-us/dotnet/visual-basic/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![Release](https://img.shields.io/github/v/release/Dmitri433/SindDll?style=flat-square)](https://github.com/Dmitri433/SindDll/releases)

**SindDll** — это библиотека для .NET Framework, представляющая собой компилятор, интерпретатор и среду выполнения для скриптов языка **Sind**.

Библиотека позволяет легко интегрировать выполнение скриптов в ваши Windows-приложения (WinForms/WPF) и включает в себя встроенный визуальный отладчик.

## ✨ Основные возможности

* ⚙️ **Компиляция и интерпретация** — выполнение скриптов Sind на лету.
* 🐞 **Встроенный отладчик** — пошаговое выполнение, точки останова и просмотр состояния.
* 🔢 **Работа с памятью** — встроенные классы для работы с нативными структурами (`NativeArray`, `NativeString`).
* 📜 **Ассемблер** — поддержка интерпретации низкоуровневых инструкций (`AsmInterpreter`).

## 📦 Установка

На данный момент библиотека распространяется в виде готовых бинарных файлов.

1. Перейдите в раздел [**Releases**](https://github.com/Dmitrii433/SindDll/releases).
2. Скачайте последнюю версию `SindDll.dll` (и `SindDll.xml` для работы IntelliSense).
3. Добавьте ссылку на `SindDll.dll` в вашем проекте Visual Studio.
4. Убедитесь, что в проекте подключены сборки `System.Windows.Forms` и `System.Drawing` (требуются для работы отладчика).

> **Требования:** .NET Framework 4.7.2 или выше, Windows.

## 🚀 Быстрый старт (Пример)

Пример того, как инициализировать и запустить простой скрипт Sind из вашего кода на VB.NET:

```vbnet
Imports SindDll

Module Program
    Sub Main()
        ' Инициализация программы Sind
        Dim program As New SindProgram()
        
        ' Добавление кода скрипта
        program.LoadScript("print 'Hello from Sind!'")
        
        ' Запуск на выполнение
        program.Execute()
    End Sub
End Module
