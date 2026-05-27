<?php
echo '<h2>PHP Settings</h2>';
echo '<strong>upload_max_filesize:</strong> ' . ini_get('upload_max_filesize') . '<br>';
echo '<strong>post_max_size:</strong> ' . ini_get('post_max_size') . '<br>';
echo '<strong>memory_limit:</strong> ' . ini_get('memory_limit') . '<br>';
echo '<strong>max_execution_time:</strong> ' . ini_get('max_execution_time') . ' seconds<br>';
echo '<hr>';
echo '<h2>WordPress Upload Limits</h2>';
echo '<p>You can upload files up to <strong style="color:green">' . ini_get('upload_max_filesize') . '</strong></p>';
?>