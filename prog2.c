#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
int main() {
	FILE* input;
	FILE* output;
	output = fopen("out.c", "w+");
	input = fopen("inp.c", "r");
	short flag = 0;
	int symbol = 0;
	int cov = 0;
	while ((symbol = fgetc(input)) != EOF)
	{
		if ((symbol == '\'' || symbol == '"') && flag == 0)
		{
			cov = symbol;
			fputc(symbol, output);
			flag = 3;
			continue;
		}
		else
			if (symbol == cov && flag == 3)
			{
				cov = 0;
				fputc(symbol, output);
				flag = 0;
				continue;
			}
		if (flag == 3 && symbol == '\\')
		{
			fputc(symbol, output);
			symbol = fgetc(input);
			if (symbol != EOF)
			{
				fputc(symbol, output);
				continue;
			}
			else
			{
				fclose(input);
				fclose(output);
				return;
			}
		}
		if (flag == 3 && symbol == '\n')
		{
			fputc(symbol, output);
			flag = 0;
			continue;
		}
		if (flag == 3)
		{
			fputc(symbol, output);
			continue;
		}
		
		if (symbol == '/' && flag == 0)
		{
			symbol = fgetc(input);
			if (symbol == '/')
			{
				flag = 1;
				continue;
			}
			else
				if (symbol == '*')
				{
					flag = 2;
					continue;
				}
				else
					ungetc(symbol, input);
		}

		if (flag == 1 && symbol != '\\' && symbol != '\n')
			continue;
		else
			if (flag == 1 && symbol == '\\')
			{
				symbol = fgetc(input);
				if (symbol != EOF)
					continue;
				else
				{
					fclose(input);
					fclose(output);
					return;
				}
			}
			else
				if (flag == 1 && symbol == '\n')
				{
					flag = 0;
					fputc(symbol, output);
					continue;
				}

		if (flag == 2 && symbol != '*')
			continue;
		else
			if (flag == 2 && symbol == '*')
			{
				symbol = fgetc(input);
				if (symbol == '/')
				{
					flag = 0;
					continue;
				}
				else
					if (symbol == EOF)
					{
						fclose(input);
						fclose(output);
						return;
					}
					else
					{
						ungetc(symbol, input);
						continue;
					}
			}

		if (flag == 0) 
		{
			fputc(symbol, output);
			continue;
		}
	}
	fclose(input);
	fclose(output);
	return 0;
}