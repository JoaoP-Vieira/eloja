CREATE TABLE public.produto (
	id uuid NOT NULL,
	nome varchar NOT NULL,
	descricao varchar NULL,
	foto varchar NULL,
	categoria_id uuid NULL,
	criado_em timestamp NOT NULL,
	modificado_em timestamp NULL,
	CONSTRAINT produto_id PRIMARY KEY (id),
	CONSTRAINT produto_nome UNIQUE (nome)
);

ALTER TABLE public.produto ADD CONSTRAINT produto_categoria_fk FOREIGN KEY (categoria_id) REFERENCES public.categoria(id) ON DELETE SET NULL;