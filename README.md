Представьте себе, что вы несколько часов\дней закачивали по сети с удаленного компьютера скажем 50Гб, 
из за мелких ошибок в соединении часть файлов могла повредится, что в такой ситуации можно сделать? 
Вариант номер 1 - проверить чексуммы каждого файла и заново загрузить поврежденные, это может помочь, 
но что если файлов 10000 или 1 файл в 50Гб, что тогда? Как раз для такой ситуации создана данная утилита - PatchGen, 
она проанализирует папку со всем ее содержимым и позволит создать патч на основе исходных данных и результатов анализа, 
который заменит только поврежденные части, которые в большинстве в сумме и 10 мб не занимают, в результате не 
придется загружать файлы заново, достаточно загрузить патч, который содержит нужные данные.

Допустим есть 2 компьютера с данными, разделенные медленным соединением (иначе этой проблемы бы не было:), 
пусть компьютер 1 содержит исходную папку, компьютер 2 скачал данные с компьютера 1 и по пути их повредил в случайных местах.
Краткая инструкция:
Шаг 1: Поместить приложение на компьютер 2, выбрать "Создать отчет о состоянии папки", нужно будет выбрать папку с поврежденными данными и выбрать куда сохранить "отчет". Отчет содержит информацию о состоянии каждого файла, по которой будет строиться патч.
Шаг 2: Поместить приложение на компьютер 1 вместе с файлом отчета, созданным на шаге 1, выбрать "Создать патч на основе отчета", выбрать папку с исходными файлами и отчет, который принесли с компьютера 2, выбрать куда сохранить патч.
Шаг 3: Поместить приложение на компьютер 2 вместе с патчем, созданным на шаге 2, выбрать "применить патч", выбрать папку с поврежденными файлами, выбрать патч. Приложение автоматически применит все изменения к файлам.
Таким образом всего в 3 шага можно "залечить" поврежденные файлы без необходимости перекачивать все файлы заново (они ведь могут опять повредиться).
Приложение прошло "боевое" тестирование через дистрибутив одной довольно известной игры в 59Гб размером, при загрузке которого получились битыми 5 файлов из 8, как оказалось патч, чтобы исправить ошибки весил всего 1,5Мб:)
После применения патча архивы распаковались без единой ошибки.