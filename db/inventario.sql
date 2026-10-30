create table public.inventario (
	id uuid not null,
	produto_id uuid not null,
	quantidade_disponivel int4 default 0 not null,
	quantidade_reservada int4 default 0 not null,
	criado_em timestamp not null,
	modificado_em timestamp null,
	constraint inventario_id primary key (id)
);
-- public.inventario chaves estrangeiras

alter table public.inventario add constraint inventario_produto_fk foreign key (produto_id) references public.produto(id) on
delete
    cascade;

create index inventario_produto_id_idx on public.inventario (produto_id);