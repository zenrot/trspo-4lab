#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
int main() {
	FILE* input = fopen("test.c", "r");
	FILE* output = fopen("test.wc", "w+");
	short flag = 0;//отвечает за состояния
	char ai = 0;
	int cov = 0;
	while ((ai = fgetc(input)) != EOF)
	{
		//printf("%d \n", flag);
		
		if (flag==1)
		{
			fputc(ai, output);
			if (ai == '"')//проверка на  строки или символа
			{ flag = 0;}
			if (ai == '\\')
			{
				ai = fgetc(input);
				fputc(ai, output);
				continue;
			}
			if (ai == '\n') {flag = 0; continue; }
			continue;

		}

		if (flag == 5)
		{
			fputc(ai, output);
			if (ai == '\'')//проверка на  строки или символа
			{flag = 0;}
			if (ai == '\n') { flag = 0; continue; }
			continue;
		}
		
		if (flag == 2) { 
			if (ai == '/') {flag = 3; continue;}
			if (ai == '*') {flag = 4; cov = 43; continue;	}
			fputc('/', output);
			flag = 0;
		}
		if (flag == 3) 
		{ 
			if (ai == '\\') { ai = fgetc(input); continue;}
			if (ai != '\n') { continue; } 
			else { fputc(ai, output); flag = 0;  continue; }
		}
		if (flag == 4) 
		{ 
			if (cov=='*'&&ai=='/'){ flag = 0; continue; }
			else { cov = ai; continue; }
		}
		/*
		if (ai == '\\') 
		{ 
			fputc(ai, output); ai = fgetc(input);
			//if (ai == '\n') 
			 fputc(ai, output); 
			continue; 
		}//в любом случае перенос строки с сохр. flag */
		
		if ((ai == '"')&& flag==0)//проверка на  строки или символа
		{
			flag = 1;//значит открыли кавычки
			fputc(ai, output);
			continue;
		}

		if ((ai == '\'') && flag == 0)//проверка на  строки или символа
		{
			flag = 5;//значит открыли кавычки
			fputc(ai, output);
			continue;
		}

		if (ai == '/' && flag==0)//
		{
			//fputc(ai, output);
			cov = ai;
			flag = 2;
			continue;
		}
		
		if (flag == 0)
		{
			fputc(ai, output);
			continue;
		}
	}
	fclose(input);
	fclose(output);
	return 0;
}