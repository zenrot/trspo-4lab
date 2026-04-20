#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#define end !feof(file)

int main() {
	FILE* project, * file, * result;
	char name_file[35], sym, bol;
	int N = 0, i = 0, k = 1, count = 1, name = 1;
	project = fopen("project.txt", "r");
	if (fscanf(project, "%d\n", &N) != NULL) {
		for (; i < N; i++) {                         //Цикл с открытием файлов
			fgets(name_file, 35, project);
			for (int j = strlen(name_file); j >= 0; j--)
				if (name_file[j] == '\n') {
					name_file[j] = '\0';
					break;
				}
			file = fopen(name_file, "r");

				for (int j = strlen(name_file); j >= 0; j--) {   
					if (name_file[j] == '.') {
						name_file[j + 1] = 'w';
						name_file[j + 2] = 'c';
						name_file[j + 3] = '\0';
						break;
					}
				}
				result = fopen(name_file, "w"); //Файл для записи    
				if (result != NULL) {

					while (end) {
						k = 1;
						sym = fgetc(file);
						if (end) {

							if (sym == '/') {
								bol = fgetc(file);
								if (bol == '*') {
									bol = fgetc(file);
									while (end) {
										if (bol == '*') {
											bol = fgetc(file);

											if (bol == '/') {
												count++;
												break;
											}
										}
										if (bol == '*') continue;
										bol = fgetc(file);
									}

									if (feof(file)) break;
									if (count != 1) {
										count = 1;
										continue;
									}
								}
									if (bol == '/') {
										while (end) {
										bol = fgetc(file);
										while (bol == '\\') {
											bol = fgetc(file);
											if (bol == '\n') {
												fputc(bol, result);
												bol = fgetc(file);
												continue;
											}
										}
										if (bol == '\n') {
											if (end) {
												fputc(bol, result);
												break;
											}
										}
										}
									}
									else {
										fputc(sym, result);
										sym = bol;
									}
							}

							if (sym == '\'') {
								if (end) fputc(sym, result);
								while (end) {
									sym = fgetc(file);
									if (end) fputc(sym, result);
									while (sym == '\\') {      
										k++;
										sym = fgetc(file);
										if (end) fputc(sym, result);
										if (k > 1 && k % 2 == 0 && sym == '\'') break;
									}
									if (k % 2 != 0 && sym == '\'') break;
									if (sym == '\n') break;	
									k = 1;
								}
								}

						if (sym == '\"') {
							if (end) fputc(sym, result);
							while (end) {
								sym = fgetc(file);
								if (end) fputc(sym, result);
								if (sym == '\"') break;
								if (sym == '\n') break;
								while (sym == '\\') {      
								k++;
									sym = fgetc(file);
									if (k % 2 != 0 && sym == '\n') break;
									if (end) fputc(sym, result);
									if (k % 2 == 0 && sym == '\"') break;
								}
								if (k % 2 != 0 && sym == '\"') break;
								if (k % 2 != 0 && sym == '\n') break;
								k = 1;
							}
						}
						if (end && sym!='\"' && sym != '\'' && sym!='/') {
							fputc(sym, result);
							continue;
						}
					}
				}
			}
		}
	}
	else return -1;
	fclose(project);
	return 0;
}