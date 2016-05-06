<?php
session_start();
header("Cache-control: No-Cache");
header("Pragma: No-Cache");

function saveFile()
{
	//global $handle;
	$filename = urldecode($_FILES["file"]["name"]);
	try
	{
		if (file_exists($filename))
		{
			echo 'existed';
		}
		else
		{
			$media=$_FILES['file']['tmp_name'];
			if($media)
			{
				move_uploaded_file($_FILES["file"]["tmp_name"], $filename);
				echo 'upok';
			}
			else
				echo 'error';
		}
	}
	catch (Exception $e) 
	{
		echo 'error';
	}	
}

// logging file handler for debugging purpose
//$handle = fopen("uploadfile.txt", "a");
//if($handle === false) exit("Cannot open uploadfile.txt");
saveFile();
//fclose($handle);
?>