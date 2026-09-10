CREATE TABLE public.categoria (
	id uuid NOT NULL,
	nome varchar NOT NULL,
	descricao varchar NULL,
	criado_em timestamp NOT NULL,
	modificado_em timestamp NULL,
	CONSTRAINT categoria_id PRIMARY KEY (id),
	CONSTRAINT categoria_nome UNIQUE (nome)
);