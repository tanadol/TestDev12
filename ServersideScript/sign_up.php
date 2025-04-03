<?php

header('Content-Type: application/json');
require_once 'config.php';

try {
    $conn = new PDO("mysql:host=$servername;dbname=$dbname", $dbusername, $dbpassword);
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    echo json_encode(["status" => "error", "message" => "Database connection failed: " . $e->getMessage()]);
    exit;
}

// Check if data is received via POST
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $username = isset($_POST['username']) ? trim($_POST['username']) : '';
    $password = isset($_POST['password']) ? trim($_POST['password']) : '';

    if (empty($username) || empty($password)) {
        echo json_encode(["status" => "error", "message" => "nousernamorpassword"]);
        exit;
    }

    // Check for duplicate username
    $stmt = $conn->prepare("SELECT COUNT(*) FROM user WHERE username = :username");
    $stmt->execute(['username' => $username]);
    if ($stmt->fetchColumn() > 0) {
        echo json_encode(["status" => "error", "message" => "signuperroruserexist"]);
        exit;
    }

    // Hash the password
    $hashedPassword = password_hash($password, PASSWORD_BCRYPT);

    // Start transaction
    $conn->beginTransaction();

    try {
        // Insert the new user into the user table
        $stmt = $conn->prepare("INSERT INTO user (username, password) VALUES (:username, :password)");
        $stmt->execute(['username' => $username, 'password' => $hashedPassword]);
        
        // Get the inserted user ID
        $userId = $conn->lastInsertId();

        // Add an entry in the user_data table
        $stmt = $conn->prepare("INSERT INTO user_data (user_id, diamond, heart) VALUES (:user_id, 1000, 100)");
        $stmt->execute(['user_id' => $userId]);

        // Commit the transaction
        $conn->commit();
        echo json_encode(["status" => "success", "message" => "signupsuccess", "user_id" => $userId]);
    } catch (PDOException $e) {
        // Rollback if any error occurs
        $conn->rollBack();
        echo json_encode(["status" => "error", "message" => "Registration failed: " . $e->getMessage()]);
    }
} else {
    echo json_encode(["status" => "error", "message" => "Invalid request method."]);
}

?>
