--
-- PostgreSQL database dump
--

-- Dumped from database version 17.5
-- Dumped by pg_dump version 17.5

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: get_customer_orders(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_customer_orders(customer_id_param integer) RETURNS TABLE(orderid integer, orderdate timestamp without time zone, totalamount numeric, status character varying, itemcount bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT o.orderid, o.orderdate, o.totalamount, o.status, COUNT(oi.orderitemid) AS itemcount
    FROM orders o
    LEFT JOIN order_items oi ON o.orderid = oi.orderid
    WHERE o.userid = customer_id_param
    GROUP BY o.orderid, o.orderdate, o.totalamount, o.status
    ORDER BY o.orderdate DESC;
END;
$$;


ALTER FUNCTION public.get_customer_orders(customer_id_param integer) OWNER TO postgres;

--
-- Name: get_low_stock_products(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_low_stock_products(threshold_param integer) RETURNS TABLE(productid integer, name character varying, totalquantity integer, portionsize integer, portionsleft integer, categoryname character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT p.productid, p.name, p.totalquantity, p.portionsize, 
           p.totalquantity / p.portionsize AS portionsleft,
           c.name AS categoryname
    FROM products p
    JOIN categories c ON p.categoryid = c.categoryid
    WHERE p.totalquantity <= threshold_param
    ORDER BY portionsleft ASC;
END;
$$;


ALTER FUNCTION public.get_low_stock_products(threshold_param integer) OWNER TO postgres;

--
-- Name: is_menu_available(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.is_menu_available(menu_id_param integer) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    unavailable_count INT;
BEGIN
    SELECT COUNT(*) INTO unavailable_count
    FROM menu_products mp
    JOIN products p ON mp.productid = p.productid
    WHERE mp.menuid = menu_id_param
    AND (p.isavailable = FALSE OR p.totalquantity < mp.portionsize);
    
    RETURN (unavailable_count = 0);
END;
$$;


ALTER FUNCTION public.is_menu_available(menu_id_param integer) OWNER TO postgres;

--
-- Name: search_products(character varying, boolean, boolean); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.search_products(search_term character varying, search_by_allergen boolean, invert_search boolean) RETURNS TABLE(productid integer, name character varying, price numeric, portionsize integer, categoryname character varying, isavailable boolean, allergens text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF search_by_allergen THEN
        IF invert_search THEN
            -- Products that DON'T contain the allergen
            RETURN QUERY
            SELECT p.productid, p.name, p.price, p.portionsize, c.name AS categoryname, p.isavailable,
                   string_agg(a.name, ', ') AS allergens
            FROM products p
            LEFT JOIN categories c ON p.categoryid = c.categoryid
            LEFT JOIN product_allergens pa ON p.productid = pa.productid
            LEFT JOIN allergens a ON pa.allergenid = a.allergenid
            GROUP BY p.productid, p.name, p.price, p.portionsize, c.name, p.isavailable
            HAVING p.productid NOT IN (
                SELECT p2.productid
                FROM products p2
                JOIN product_allergens pa2 ON p2.productid = pa2.productid
                JOIN allergens a2 ON pa2.allergenid = a2.allergenid
                WHERE LOWER(a2.name) LIKE LOWER('%' || search_term || '%')
            );
        ELSE
            -- Products that DO contain the allergen
            RETURN QUERY
            SELECT p.productid, p.name, p.price, p.portionsize, c.name AS categoryname, p.isavailable,
                   string_agg(a.name, ', ') AS allergens
            FROM products p
            LEFT JOIN categories c ON p.categoryid = c.categoryid
            LEFT JOIN product_allergens pa ON p.productid = pa.productid
            LEFT JOIN allergens a ON pa.allergenid = a.allergenid
            WHERE p.productid IN (
                SELECT p2.productid
                FROM products p2
                JOIN product_allergens pa2 ON p2.productid = pa2.productid
                JOIN allergens a2 ON pa2.allergenid = a2.allergenid
                WHERE LOWER(a2.name) LIKE LOWER('%' || search_term || '%')
            )
            GROUP BY p.productid, p.name, p.price, p.portionsize, c.name, p.isavailable;
        END IF;
    ELSE
        IF invert_search THEN
            -- Products that DON'T contain the search term in name
            RETURN QUERY
            SELECT p.productid, p.name, p.price, p.portionsize, c.name AS categoryname, p.isavailable,
                   string_agg(a.name, ', ') AS allergens
            FROM products p
            LEFT JOIN categories c ON p.categoryid = c.categoryid
            LEFT JOIN product_allergens pa ON p.productid = pa.productid
            LEFT JOIN allergens a ON pa.allergenid = a.allergenid
            WHERE LOWER(p.name) NOT LIKE LOWER('%' || search_term || '%')
            GROUP BY p.productid, p.name, p.price, p.portionsize, c.name, p.isavailable;
        ELSE
            -- Products that DO contain the search term in name
            RETURN QUERY
            SELECT p.productid, p.name, p.price, p.portionsize, c.name AS categoryname, p.isavailable,
                   string_agg(a.name, ', ') AS allergens
            FROM products p
            LEFT JOIN categories c ON p.categoryid = c.categoryid
            LEFT JOIN product_allergens pa ON p.productid = pa.productid
            LEFT JOIN allergens a ON pa.allergenid = a.allergenid
            WHERE LOWER(p.name) LIKE LOWER('%' || search_term || '%')
            GROUP BY p.productid, p.name, p.price, p.portionsize, c.name, p.isavailable;
        END IF;
    END IF;
END;
$$;


ALTER FUNCTION public.search_products(search_term character varying, search_by_allergen boolean, invert_search boolean) OWNER TO postgres;

--
-- Name: update_product_availability(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.update_product_availability() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Update isavailable flag based on stock level
    UPDATE products
    SET isavailable = CASE WHEN NEW.totalquantity >= NEW.portionsize THEN TRUE ELSE FALSE END
    WHERE productid = NEW.productid;
    
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_product_availability() OWNER TO postgres;

--
-- Name: update_product_quantities_for_order(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.update_product_quantities_for_order(order_id_param integer) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    item RECORD;
    product_portion INT;
    item_quantity INT;
    quantity_to_deduct INT;
BEGIN
    -- Process regular products in the order
    FOR item IN 
        SELECT oi.productid, oi.quantity, p.portionsize
        FROM order_items oi
        JOIN products p ON oi.productid = p.productid
        WHERE oi.orderid = order_id_param AND oi.productid IS NOT NULL
    LOOP
        quantity_to_deduct := item.quantity * item.portionsize;
        
        -- Check if enough quantity is available
        IF (SELECT totalquantity FROM products WHERE productid = item.productid) < quantity_to_deduct THEN
            RETURN FALSE;
        END IF;
        
        -- Update product quantity
        UPDATE products
        SET totalquantity = totalquantity - quantity_to_deduct,
            isavailable = CASE WHEN (totalquantity - quantity_to_deduct) <= 0 THEN FALSE ELSE isavailable END
        WHERE productid = item.productid;
    END LOOP;
    
    -- Process menu items in the order
    FOR item IN 
        SELECT mp.productid, oi.quantity, mp.portionsize
        FROM order_items oi
        JOIN menu_products mp ON oi.menuid = mp.menuid
        WHERE oi.orderid = order_id_param AND oi.menuid IS NOT NULL
    LOOP
        quantity_to_deduct := item.quantity * item.portionsize;
        
        -- Check if enough quantity is available
        IF (SELECT totalquantity FROM products WHERE productid = item.productid) < quantity_to_deduct THEN
            RETURN FALSE;
        END IF;
        
        -- Update product quantity
        UPDATE products
        SET totalquantity = totalquantity - quantity_to_deduct,
            isavailable = CASE WHEN (totalquantity - quantity_to_deduct) <= 0 THEN FALSE ELSE isavailable END
        WHERE productid = item.productid;
    END LOOP;
    
    RETURN TRUE;
END;
$$;


ALTER FUNCTION public.update_product_quantities_for_order(order_id_param integer) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: allergens; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.allergens (
    allergenid integer NOT NULL,
    name character varying(50) NOT NULL,
    description text
);


ALTER TABLE public.allergens OWNER TO postgres;

--
-- Name: allergens_allergenid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.allergens_allergenid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.allergens_allergenid_seq OWNER TO postgres;

--
-- Name: allergens_allergenid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.allergens_allergenid_seq OWNED BY public.allergens.allergenid;


--
-- Name: cart_items; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cart_items (
    cartitemid integer NOT NULL,
    userid integer NOT NULL,
    productid integer NOT NULL,
    quantity integer NOT NULL,
    dateadded timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.cart_items OWNER TO postgres;

--
-- Name: cart_items_cartitemid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.cart_items_cartitemid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cart_items_cartitemid_seq OWNER TO postgres;

--
-- Name: cart_items_cartitemid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.cart_items_cartitemid_seq OWNED BY public.cart_items.cartitemid;


--
-- Name: categories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.categories (
    categoryid integer NOT NULL,
    name character varying(100) NOT NULL,
    description text
);


ALTER TABLE public.categories OWNER TO postgres;

--
-- Name: categories_categoryid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.categories_categoryid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.categories_categoryid_seq OWNER TO postgres;

--
-- Name: categories_categoryid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.categories_categoryid_seq OWNED BY public.categories.categoryid;


--
-- Name: menu_products; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.menu_products (
    menuid integer NOT NULL,
    productid integer NOT NULL,
    portionsize integer NOT NULL
);


ALTER TABLE public.menu_products OWNER TO postgres;

--
-- Name: menus; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.menus (
    menuid integer NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    price numeric(10,2) NOT NULL,
    categoryid integer,
    isavailable boolean DEFAULT true
);


ALTER TABLE public.menus OWNER TO postgres;

--
-- Name: menus_menuid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.menus_menuid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.menus_menuid_seq OWNER TO postgres;

--
-- Name: menus_menuid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.menus_menuid_seq OWNED BY public.menus.menuid;


--
-- Name: order_items; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.order_items (
    orderitemid integer NOT NULL,
    orderid integer,
    productid integer,
    menuid integer,
    quantity integer NOT NULL,
    unitprice numeric(10,2) NOT NULL,
    CONSTRAINT order_items_check CHECK (((productid IS NOT NULL) OR (menuid IS NOT NULL)))
);


ALTER TABLE public.order_items OWNER TO postgres;

--
-- Name: order_items_orderitemid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.order_items_orderitemid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.order_items_orderitemid_seq OWNER TO postgres;

--
-- Name: order_items_orderitemid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.order_items_orderitemid_seq OWNED BY public.order_items.orderitemid;


--
-- Name: orders; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.orders (
    orderid integer NOT NULL,
    userid integer,
    orderdate timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    totalamount numeric(10,2) NOT NULL,
    deliveryfee numeric(10,2) DEFAULT 0,
    discount numeric(10,2) DEFAULT 0,
    status character varying(20) DEFAULT 'Registered'::character varying NOT NULL,
    estimateddeliverytime timestamp without time zone,
    deliveryaddress text NOT NULL
);


ALTER TABLE public.orders OWNER TO postgres;

--
-- Name: orders_orderid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.orders_orderid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.orders_orderid_seq OWNER TO postgres;

--
-- Name: orders_orderid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.orders_orderid_seq OWNED BY public.orders.orderid;


--
-- Name: product_allergens; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.product_allergens (
    productid integer NOT NULL,
    allergenid integer NOT NULL
);


ALTER TABLE public.product_allergens OWNER TO postgres;

--
-- Name: products; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.products (
    productid integer NOT NULL,
    name character varying(100) NOT NULL,
    price numeric(10,2) NOT NULL,
    portionsize integer NOT NULL,
    totalquantity integer NOT NULL,
    categoryid integer,
    isavailable boolean DEFAULT true
);


ALTER TABLE public.products OWNER TO postgres;

--
-- Name: products_productid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.products_productid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.products_productid_seq OWNER TO postgres;

--
-- Name: products_productid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.products_productid_seq OWNED BY public.products.productid;


--
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    userid integer NOT NULL,
    firstname character varying(50) NOT NULL,
    lastname character varying(50) NOT NULL,
    email character varying(100) NOT NULL,
    passwordhash character varying(255) NOT NULL,
    phonenumber character varying(20),
    deliveryaddress text,
    usertype character varying(20) NOT NULL
);


ALTER TABLE public.users OWNER TO postgres;

--
-- Name: users_userid_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.users_userid_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_userid_seq OWNER TO postgres;

--
-- Name: users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.users_userid_seq OWNED BY public.users.userid;


--
-- Name: allergens allergenid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.allergens ALTER COLUMN allergenid SET DEFAULT nextval('public.allergens_allergenid_seq'::regclass);


--
-- Name: cart_items cartitemid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cart_items ALTER COLUMN cartitemid SET DEFAULT nextval('public.cart_items_cartitemid_seq'::regclass);


--
-- Name: categories categoryid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categories ALTER COLUMN categoryid SET DEFAULT nextval('public.categories_categoryid_seq'::regclass);


--
-- Name: menus menuid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menus ALTER COLUMN menuid SET DEFAULT nextval('public.menus_menuid_seq'::regclass);


--
-- Name: order_items orderitemid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.order_items ALTER COLUMN orderitemid SET DEFAULT nextval('public.order_items_orderitemid_seq'::regclass);


--
-- Name: orders orderid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders ALTER COLUMN orderid SET DEFAULT nextval('public.orders_orderid_seq'::regclass);


--
-- Name: products productid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products ALTER COLUMN productid SET DEFAULT nextval('public.products_productid_seq'::regclass);


--
-- Name: users userid; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN userid SET DEFAULT nextval('public.users_userid_seq'::regclass);


--
-- Data for Name: allergens; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.allergens (allergenid, name, description) FROM stdin;
1	Gluten	Contains wheat gluten
2	Lactose	Contains dairy products
3	Nuts	Contains nuts or nut products
4	Eggs	Contains eggs or egg products
5	Seafood	Contains fish or shellfish
6	Soy	Contains soy or soy products
7	Celery	Contains celery or celery products
\.


--
-- Data for Name: cart_items; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.cart_items (cartitemid, userid, productid, quantity, dateadded) FROM stdin;
\.


--
-- Data for Name: categories; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.categories (categoryid, name, description) FROM stdin;
1	Breakfast	Morning meals and breakfast options
2	Appetizers	Starters and appetizers
3	Soups	Soups and broths
4	Main Course	Main dish options
5	Desserts	Sweet treats and desserts
6	Beverages	Drinks and refreshments
\.


--
-- Data for Name: menu_products; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.menu_products (menuid, productid, portionsize) FROM stdin;
1	1	400
1	24	250
2	9	300
2	8	120
3	17	350
3	18	200
3	23	330
4	14	250
4	6	150
4	20	100
5	14	250
5	15	250
5	16	250
5	6	300
5	23	330
5	20	120
\.


--
-- Data for Name: menus; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.menus (menuid, name, description, price, categoryid, isavailable) FROM stdin;
1	Breakfast Special	English breakfast with coffee or tea	16.99	1	t
2	Soup & Bread Combo	Any soup with garlic bread	12.99	3	t
3	Burger Meal	Cheeseburger with fries and drink	17.99	4	t
4	Steak Dinner	Beef steak with side salad and dessert	29.99	4	t
5	Family Bundle	Selection of main courses, sides and drinks for 4	59.99	4	t
\.


--
-- Data for Name: order_items; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.order_items (orderitemid, orderid, productid, menuid, quantity, unitprice) FROM stdin;
1	1	9	\N	1	8.99
2	1	11	\N	1	9.99
3	1	8	\N	1	6.99
4	2	17	\N	1	13.99
5	2	18	\N	1	15.99
6	2	23	\N	1	2.99
7	3	\N	3	1	17.99
8	4	\N	3	2	17.99
9	4	19	\N	1	7.99
10	4	20	\N	1	6.99
11	4	23	\N	1	2.99
12	5	\N	4	1	29.99
13	6	\N	5	1	59.99
14	7	5	\N	1	7.99
15	8	5	\N	1	7.99
16	8	6	\N	1	9.99
17	8	8	\N	1	6.99
18	9	5	\N	1	7.99
19	9	6	\N	1	9.99
20	9	8	\N	1	6.99
21	10	5	\N	3	7.99
22	10	6	\N	1	9.99
23	10	8	\N	1	6.99
24	11	5	\N	1	7.99
25	12	5	\N	1	7.99
\.


--
-- Data for Name: orders; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.orders (orderid, userid, orderdate, totalamount, deliveryfee, discount, status, estimateddeliverytime, deliveryaddress) FROM stdin;
1	3	2025-05-17 10:08:52.901108	25.98	0.00	0.00	Delivered	2025-05-17 11:08:52.901108	123 Main St, Anytown
2	3	2025-05-18 10:08:52.901108	32.97	5.00	2.50	Delivered	2025-05-18 12:08:52.901108	123 Main St, Anytown
4	4	2025-05-16 10:08:52.901108	54.97	0.00	5.00	Delivered	2025-05-16 11:08:52.901108	456 Oak Ave, Somewhere
5	4	2025-05-19 08:08:52.901108	29.99	0.00	0.00	Preparing	2025-05-19 10:48:52.901108	456 Oak Ave, Somewhere
6	5	2025-05-19 06:08:52.901108	59.99	0.00	5.00	Registered	2025-05-19 11:03:52.901108	789 Pine Rd, Nowhere
8	6	2025-05-19 16:31:05.334445	29.97	5.00	0.00	Cancelled	2025-05-19 17:31:05.332671	Memo 32
7	6	2025-05-19 16:29:07.460703	12.99	5.00	0.00	Cancelled	2025-05-19 17:29:07.458991	Memo 32
11	6	2025-05-19 22:27:07.187131	12.99	5.00	0.00	Registered	2025-05-19 23:27:07.185459	Memo 32
9	6	2025-05-19 16:39:41.532462	29.97	5.00	0.00	Cancelled	2025-05-19 17:39:41.530715	Memo 32
10	7	2025-05-19 17:27:00.204226	45.95	5.00	0.00	Cancelled	2025-05-19 18:27:00.202605	cox
12	1	2025-05-19 22:28:47.425185	12.99	5.00	0.00	Registered	2025-05-19 23:28:47.424963	Restaurant Address
3	3	2025-05-19 05:08:52.901108	17.99	0.00	0.00	Cancelled	2025-05-19 10:33:52.901108	123 Main St, Anytown
\.


--
-- Data for Name: product_allergens; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.product_allergens (productid, allergenid) FROM stdin;
1	1
1	2
1	4
2	1
2	2
2	4
3	1
4	2
4	3
5	1
6	2
7	1
7	2
7	4
8	1
8	2
9	2
9	7
10	7
11	1
11	7
12	2
13	5
14	2
15	1
15	2
15	4
16	6
17	1
17	2
17	4
18	1
18	5
19	2
19	4
20	1
20	2
20	4
21	1
22	2
22	4
\.


--
-- Data for Name: products; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.products (productid, name, price, portionsize, totalquantity, categoryid, isavailable) FROM stdin;
1	English Breakfast	14.99	450	45000	1	t
2	Pancakes with Maple Syrup	9.99	300	30000	1	t
3	Avocado Toast	11.99	250	25000	1	t
4	Yogurt with Granola	8.99	200	20000	1	t
5	Bruschetta	7.99	150	15000	2	t
6	Caprese Salad	9.99	200	20000	2	t
7	Mozzarella Sticks	8.99	180	18000	2	t
8	Garlic Bread	6.99	120	12000	2	t
9	Mushroom Cream Soup	8.99	300	30000	3	t
10	Vegetable Soup	7.99	300	30000	3	t
11	Chicken Noodle Soup	9.99	350	35000	3	t
12	Tomato Soup	7.99	300	30000	3	t
13	Grilled Salmon	18.99	250	25000	4	t
14	Beef Steak	22.99	300	30000	4	t
15	Chicken Parmesan	16.99	350	35000	4	t
16	Vegetable Stir Fry	14.99	300	30000	4	t
17	Cheeseburger	13.99	350	35000	4	t
18	Fish & Chips	15.99	400	40000	4	t
19	Tiramisu	7.99	150	15000	5	t
20	Chocolate Cake	6.99	120	12000	5	t
21	Apple Pie	6.99	150	15000	5	t
22	Ice Cream Sundae	5.99	200	20000	5	t
23	Coca Cola	2.99	330	33000	6	t
24	Sprite	2.99	330	33000	6	t
25	Lemonade	3.99	300	30000	6	t
26	Coffee	3.99	250	25000	6	t
27	Tea	2.99	250	25000	6	t
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.users (userid, firstname, lastname, email, passwordhash, phonenumber, deliveryaddress, usertype) FROM stdin;
1	Admin	User	admin@restaurant.com	8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918	1234567890	Restaurant Address	Employee
2	John	Manager	john@restaurant.com	5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5	1234567891	Restaurant Address	Employee
3	Alice	Smith	alice@example.com	5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5	9876543210	123 Main St, Anytown	Customer
4	Bob	Johnson	bob@example.com	5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5	9876543211	456 Oak Ave, Somewhere	Customer
5	Carol	Williams	carol@example.com	5994471abb01112afcc18159f6cc74b4f511b99806da59b3caf5a9c173cacfc5	9876543212	789 Pine Rd, Nowhere	Customer
6	Costin	Ghiujan	costin.ghiujan@gmail.com	5143632adef8429196cb0cc61477b31899947e6d7b35ff598246fc0f67d6908f	123456	Memo 32	Customer
7	cox	cox	cox	ce8cf2b15e7cac6d5ef34e9bd3efeaeca8abb776059ad805274debc3aa58e290	666	cox	Customer
\.


--
-- Name: allergens_allergenid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.allergens_allergenid_seq', 7, true);


--
-- Name: cart_items_cartitemid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.cart_items_cartitemid_seq', 18, true);


--
-- Name: categories_categoryid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.categories_categoryid_seq', 7, true);


--
-- Name: menus_menuid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.menus_menuid_seq', 5, true);


--
-- Name: order_items_orderitemid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.order_items_orderitemid_seq', 25, true);


--
-- Name: orders_orderid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.orders_orderid_seq', 12, true);


--
-- Name: products_productid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.products_productid_seq', 27, true);


--
-- Name: users_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_userid_seq', 7, true);


--
-- Name: allergens allergens_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.allergens
    ADD CONSTRAINT allergens_pkey PRIMARY KEY (allergenid);


--
-- Name: cart_items cart_items_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_pkey PRIMARY KEY (cartitemid);


--
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (categoryid);


--
-- Name: menu_products menu_products_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menu_products
    ADD CONSTRAINT menu_products_pkey PRIMARY KEY (menuid, productid);


--
-- Name: menus menus_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menus
    ADD CONSTRAINT menus_pkey PRIMARY KEY (menuid);


--
-- Name: order_items order_items_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_pkey PRIMARY KEY (orderitemid);


--
-- Name: orders orders_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_pkey PRIMARY KEY (orderid);


--
-- Name: product_allergens product_allergens_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.product_allergens
    ADD CONSTRAINT product_allergens_pkey PRIMARY KEY (productid, allergenid);


--
-- Name: products products_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (productid);


--
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


--
-- Name: idx_cart_items_user; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cart_items_user ON public.cart_items USING btree (userid);


--
-- Name: idx_menu_products_menu; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_menu_products_menu ON public.menu_products USING btree (menuid);


--
-- Name: idx_menus_category; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_menus_category ON public.menus USING btree (categoryid);


--
-- Name: idx_order_items_order; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_order_items_order ON public.order_items USING btree (orderid);


--
-- Name: idx_orders_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_orders_status ON public.orders USING btree (status);


--
-- Name: idx_orders_user; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_orders_user ON public.orders USING btree (userid);


--
-- Name: idx_product_allergens_product; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_product_allergens_product ON public.product_allergens USING btree (productid);


--
-- Name: idx_products_category; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_products_category ON public.products USING btree (categoryid);


--
-- Name: products product_quantity_update_trigger; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER product_quantity_update_trigger AFTER UPDATE OF totalquantity ON public.products FOR EACH ROW EXECUTE FUNCTION public.update_product_availability();


--
-- Name: cart_items cart_items_productid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_productid_fkey FOREIGN KEY (productid) REFERENCES public.products(productid) ON DELETE CASCADE;


--
-- Name: cart_items cart_items_userid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cart_items
    ADD CONSTRAINT cart_items_userid_fkey FOREIGN KEY (userid) REFERENCES public.users(userid) ON DELETE CASCADE;


--
-- Name: menu_products menu_products_menuid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menu_products
    ADD CONSTRAINT menu_products_menuid_fkey FOREIGN KEY (menuid) REFERENCES public.menus(menuid) ON DELETE CASCADE;


--
-- Name: menu_products menu_products_productid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menu_products
    ADD CONSTRAINT menu_products_productid_fkey FOREIGN KEY (productid) REFERENCES public.products(productid) ON DELETE CASCADE;


--
-- Name: menus menus_categoryid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.menus
    ADD CONSTRAINT menus_categoryid_fkey FOREIGN KEY (categoryid) REFERENCES public.categories(categoryid);


--
-- Name: order_items order_items_menuid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_menuid_fkey FOREIGN KEY (menuid) REFERENCES public.menus(menuid);


--
-- Name: order_items order_items_orderid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_orderid_fkey FOREIGN KEY (orderid) REFERENCES public.orders(orderid) ON DELETE CASCADE;


--
-- Name: order_items order_items_productid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.order_items
    ADD CONSTRAINT order_items_productid_fkey FOREIGN KEY (productid) REFERENCES public.products(productid);


--
-- Name: orders orders_userid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_userid_fkey FOREIGN KEY (userid) REFERENCES public.users(userid);


--
-- Name: product_allergens product_allergens_allergenid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.product_allergens
    ADD CONSTRAINT product_allergens_allergenid_fkey FOREIGN KEY (allergenid) REFERENCES public.allergens(allergenid) ON DELETE CASCADE;


--
-- Name: product_allergens product_allergens_productid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.product_allergens
    ADD CONSTRAINT product_allergens_productid_fkey FOREIGN KEY (productid) REFERENCES public.products(productid) ON DELETE CASCADE;


--
-- Name: products products_categoryid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_categoryid_fkey FOREIGN KEY (categoryid) REFERENCES public.categories(categoryid);


--
-- PostgreSQL database dump complete
--

