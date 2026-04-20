#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct source_structT {
	int size;
	char* source_code;
} source_struct;

source_struct* readSource(char* source_file);
void passTextInQuotes(char quote, source_struct* source_info, int* i,
	char* result_code, int* result_size);
void deleteDoubleSlash(source_struct* source_info, int* i,
	char* result_code, int* result_size);
void deleteSlashAsterisk(source_struct* source_info, int* i,
	char* result_code, int* result_size);
char* deleteCommas(source_struct* source_info, int* result_size);
int calculateSize(FILE* file, source_struct* new_source_struct, char* source_file);

int main()
{
	FILE* project = fopen("project.txt", "r");
	int N;
	fscanf(project, "%d", &N);
	for (int i = 0; i < N; i++) {
		char source_file[33];
		char result_file[34];
		memset(source_file, 0, 33);
		fscanf(project, "%s", source_file);
		source_struct* source_info = readSource(source_file);
		int result_size;
		char* result_code = deleteCommas(source_info, &result_size);
		memset(result_file, 0, 34);
		memcpy(result_file, source_file, strlen(source_file)-1);
		strcat(result_file, "wc");
		result_file[33] = '\0';
		FILE* fout = fopen(result_file, "wb");
		fwrite(result_code, 1, result_size, fout);
		fclose(fout);
		free(result_code);
		free(source_info->source_code);
		free(source_info);
	}
	fclose(project);
}


source_struct* readSource(char* source_file) {
	FILE* fp;
	source_struct* new_source_struct = (source_struct*)malloc(sizeof(source_struct));
	if (new_source_struct == NULL) return NULL;
	fp = fopen(source_file, "rb");
	calculateSize(fp, new_source_struct, source_file);
	fseek(fp, 0, SEEK_SET);
	new_source_struct->source_code = (char*)malloc(new_source_struct->size + 1);
	if (new_source_struct->source_code == NULL) return NULL;
	fread(new_source_struct->source_code, 1, new_source_struct->size, fp);
	new_source_struct->source_code[new_source_struct->size] = '\0';
	fclose(fp);
	return new_source_struct;
}
int calculateSize(FILE* file, source_struct* new_source_struct, char* source_file) {
	fseek(file, 0, SEEK_END);
	new_source_struct->size = ftell(file);
	return new_source_struct->size;
}
void passTextInQuotes(char quote, source_struct* source_info, int* i,
	char* result_code, int* result_size) {
	result_code[*result_size] = source_info->source_code[*i];
	(*i)++;
	(*result_size)++;
	while ((*i) < source_info->size) {
		if ((source_info->source_code[(*i)] == quote) || (source_info->source_code[(*i)] == '\0')
			|| (source_info->source_code[(*i)] == '\n')) {
			result_code[*result_size] = source_info->source_code[(*i)];
			break;
		}
		if (source_info->source_code[(*i)] == '\\') {
			result_code[*result_size] = source_info->source_code[(*i)];
			(*i)++;
			(*result_size)++;
			if (source_info->source_code[(*i)] == '\\') {
				result_code[*result_size] = source_info->source_code[(*i)];
				(*i)++;
				(*result_size)++;
				continue;
			}
			if (source_info->source_code[(*i)] == quote) {
				result_code[*result_size] = source_info->source_code[(*i)];
				(*i)++;
				(*result_size)++;
				continue;
			}
			if ((source_info->source_code[(*i)] == '\n') || (source_info->source_code[(*i)] == '\0') || (source_info->source_code[(*i)] == '\r')) {
				result_code[*result_size] = source_info->source_code[(*i)];
				(*i)++;
				(*result_size)++;
				result_code[*result_size] = source_info->source_code[(*i)];
				(*i)++;
				(*result_size)++;
				continue;
			}
			continue;
		}
		result_code[*result_size] = source_info->source_code[(*i)];
		(*i)++;
		(*result_size)++;
	}
}
void deleteDoubleSlash(source_struct* source_info, int* i,
	char* result_code, int* result_size) {
	(*i) += 2;

	while ((source_info->source_code[*i] != '\0') &&
		(source_info->source_code[*i] != '\n')) {
		if ((source_info->source_code[*i] == '\\') &&
			((source_info->source_code[(*i) + 1] == '\r') ||
				(source_info->source_code[(*i) + 1] == '\n') ||
				(source_info->source_code[(*i) + 1] == '\0'))) {
			(*i)++;
			(*i)++;
		}
		(*i)++;
	}
	result_code[*result_size] = '\n';
}
void deleteSlashAsterisk(source_struct* source_info, int* i,
	char* result_code, int* result_size) {
	(*i) += 2;
	while (!((source_info->source_code[*i] == '*') &&
		(source_info->source_code[(*i) + 1] == '/')) &&
		(*i < source_info->size)) (*i)++;
	(*i)++;
	result_code[*result_size] = '\n';
}
char* deleteCommas(source_struct* source_info, int* result_size) {
	char* result_code = (char*)malloc(source_info->size);
	if (result_code == NULL) return NULL;
	*result_size = 0;
	for (int i = 0; i < source_info->size; i++, (*result_size)++) {
		if (source_info->source_code[i] == '\"')	passTextInQuotes('\"', source_info, &i, result_code, result_size);
		else if (source_info->source_code[i] == '\'')	passTextInQuotes('\'', source_info, &i, result_code, result_size);
		else if (source_info->source_code[i] == '/') {
			if (source_info->source_code[i + 1] == '*')		deleteSlashAsterisk(source_info, &i, result_code, result_size);
			else if (source_info->source_code[i + 1] == '/')	deleteDoubleSlash(source_info, &i, result_code, result_size);
			else	result_code[*result_size] = source_info->source_code[i];
		}
		else result_code[*result_size] = source_info->source_code[i];
	}
	return result_code;
}