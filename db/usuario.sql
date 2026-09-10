CREATE TABLE public."usuario" (
	id int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	email varchar NOT NULL,
	nome varchar NOT NULL,
	sobrenome varchar NOT NULL,
	password_hash varchar NOT NULL,
	CONSTRAINT usuario_id PRIMARY KEY (id),
	CONSTRAINT usuario_email UNIQUE (email)
);
