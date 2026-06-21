<?php
/** WPronto config */
define( 'DB_NAME', 'mynewsite_db' );
define( 'DB_USER', 'root' );
define( 'DB_PASSWORD', '' );
define( 'DB_HOST', '127.0.0.1' );
define( 'DB_CHARSET', 'utf8mb4' );
define( 'DB_COLLATE', '' );

if ( ! defined( 'WP_CLI' ) ) {
    define( 'WP_SITEURL', 'http://mynewsite.wp:8080' );
    define( 'WP_HOME',    'http://mynewsite.wp:8080' );
}

define( 'AUTH_KEY',         '+<athNf^M(<IsZbROgjOmIYjrRAKJQr-!)?JQ^tttkkm2NZS_rIg7tZTBFIt>-Xn' );
define( 'SECURE_AUTH_KEY',  '3_*Bxy0wQmsVw)G29exPv#?Gwz1LQG5OT(wp5+jyBCnzEwX0)P=g4a9Xe8Uv!!Ma' );
define( 'LOGGED_IN_KEY',    'ujly8nX+*R*bgu9$uC08nBs55oiP4H$KoI6eCC^ekSO*A9v>ly=gmDz#FGStK8nn' );
define( 'NONCE_KEY',        'JVR@8S5bHtNLAj9wq9rGqJctKGAz9^_hg<yJEtN27EJ(qMKbYalE?PoGzyvYS<B^' );
define( 'AUTH_SALT',        'LqP0PBJ4DNxaiD)e1Cy!K=&htkukbYR!U&zUKC=dCDw?o(a(WhZ(iBt>nwatn^8d' );
define( 'SECURE_AUTH_SALT', '>lzr)3yJ+Q@=$jpHFgq5L@=p%aW9C(Qb3fdFKIpuQ0Q%2T^C1&8n?OZhn$uBNyoN' );
define( 'LOGGED_IN_SALT',   '22b^$Ko4XQq2NW6(IrrqJMt-b_zcPl*c>sR0dIomst#7VmblqacEi+@YRvNdZKcS' );
define( 'NONCE_SALT',       '0w@_og^8gZKmI006S#P7Teo6YhEO0ceu@ZXplN1o*xX5)JIU@EVoKw3w2?1(yh%6' );

$table_prefix = 'wp_';
define( 'WP_DEBUG', false );
define( 'WP_DEBUG_DISPLAY', false );
define( 'WP_MEMORY_LIMIT', '512M' );
define( 'WP_MAX_MEMORY_LIMIT', '512M' );

if ( ! defined( 'ABSPATH' ) ) define( 'ABSPATH', __DIR__ . '/' );
require_once ABSPATH . 'wp-settings.php';